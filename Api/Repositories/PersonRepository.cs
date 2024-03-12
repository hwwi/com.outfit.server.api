using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Amazon.Auth.AccessControlPolicy;
using Api.Data;
using Api.Data.Args;
using Api.Data.Dto;
using Api.Data.Models;
using Api.Data.Payload;
using Api.Errors;
using Api.Extension;
using Api.Properties;
using Api.Service;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Castle.Core.Internal;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace Api.Repositories
{
    public class PersonRepository : AbstractEntityRepository<Person>
    {
        private readonly ILogger<PersonRepository> _logger;
        private readonly IMapper _mapper;
        private readonly VerificationService _verificationService;
        private readonly AuthenticationService _authenticationService;
        private readonly CloudStorageService _cloudStorageService;
        private readonly ImageService _imageService;

        public PersonRepository(
            ILogger<PersonRepository> logger,
            OutfitDbContext context,
            IMapper mapper,
            VerificationService verificationService,
            AuthenticationService authenticationService,
            CloudStorageService cloudStorageService,
            ImageService imageService
        ) : base(context)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _verificationService = verificationService ?? throw new ArgumentNullException(nameof(verificationService));
            _authenticationService =
                authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
            _cloudStorageService = cloudStorageService ?? throw new ArgumentNullException(nameof(cloudStorageService));
            _imageService = imageService ?? throw new ArgumentNullException(nameof(imageService));
        }

        public async Task<PersonDto?> FindOneToDtoByAsync(Expression<Func<Person, bool>> predicate)
        {
            return await Set.Where(predicate)
                .ProjectTo<PersonDto>(_mapper.ConfigurationProvider)
                .SingleOrDefaultAsync();
        }

        public async Task<PersonDetailDto?> FindOneToProfileDtoByAsync(long viewerId,
            Expression<Func<Person, bool>> predicate)
        {
            return await Set.Where(predicate)
                .ProjectTo<PersonDetailDto>(_mapper.ConfigurationProvider, new {viewerId})
                .SingleOrDefaultAsync();
        }

        public async Task<Connection<PersonDto>> FindFollowerConnection(
            ConnectionArgs args,
            long personId,
            string? keyword
        )
        {
            return await Context.FollowPersons
                .Where(x => x.FollowedId == personId)
                .Select(x => x.Follower)
                .Where(x =>
                    keyword.IsNullOrEmpty() || x.Name.StartsWith(keyword)
                )
                .ToConnectionAsync<Person, PersonDto>(
                    args,
                    _mapper
                );
        }

        public async Task<Connection<PersonDto>> FindFollowingConnection(
            ConnectionArgs args,
            long personId,
            string? keyword
        )
        {
            return await Context.FollowPersons
                .Where(x => x.FollowerId == personId)
                .Select(x => x.Followed)
                .Where(x =>
                    keyword.IsNullOrEmpty() || x.Name.StartsWith(keyword)
                )
                .ToConnectionAsync<Person, PersonDto>(
                    args,
                    _mapper
                );
        }


        public async Task<List<PersonDto>> FindToDtoByAsync(Expression<Func<Person, bool>> predicate, int? take = null)
        {
            var query = Set.Where(predicate);
            if (take != null)
                query = query.Take(take.Value);
            return await query
                .ProjectTo<PersonDto>(_mapper.ConfigurationProvider)
                .ToListAsync();
        }

        public async Task<List<PersonDetailDto>> FindToProfileDtoByAsync(
            long viewerId,
            Expression<Func<Person, bool>> predicate,
            int take)
        {
            return await Set.Where(predicate)
                .Include(x => x.Followers)
                .Take(take)
                .ProjectTo<PersonDetailDto>(_mapper.ConfigurationProvider, new {viewerId})
                .ToListAsync();
        }

        public async Task<Person> Update(
            long personId,
            string? biography
        )
        {
            Person? person = await Context.Persons
                .AsTracking()
                .FirstOrDefaultAsync(x => x.Id == personId);
            if (person == null)
                throw new ProblemDetailsException {
                    StatusCode = StatusCodes.Status404NotFound, Detail = Resources.Not_exists_person_
                };

            bool isChanged = false;

            if (biography != null && person.Biography != biography)
            {
                person.Biography = biography;
                isChanged = true;
            }

            if (isChanged)
                await Context.SaveChangesAsync();

            return person;
        }

        public async Task<Person> ChangeName(
            long personId,
            string name
        )
        {
            var person = await Context.Persons
                .AsTracking()
                .FirstOrDefaultAsync(x => x.Id == personId);

            if (person == null)
                throw new ProblemDetailsException {
                    StatusCode = StatusCodes.Status400BadRequest, Detail = Resources.Not_exists_person_
                };
            if (person.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                throw new ProblemDetailsException {
                    StatusCode = StatusCodes.Status400BadRequest,
                    Detail = Resources.The_name_you_want_to_change_is_the_same_as_before_
                };

            if (person.LastNameUpdatedAt != null)
            {
                var personLastNameUpdatedAt = person.LastNameUpdatedAt.Value;
                if (personLastNameUpdatedAt.Subtract(DateTimeOffset.UtcNow).Days <= 15)
                    throw new ProblemDetailsException {
                        StatusCode = StatusCodes.Status400BadRequest,
                        Detail = string.Format(
                            CultureInfo.CurrentCulture,
                            Resources.The_name_can_be_changed_once_every__0__days___Last_changed_at__1__,
                            15,
                            personLastNameUpdatedAt
                        )
                    };
            }

            person.Name = name;
            person.LastNameUpdatedAt = DateTimeOffset.UtcNow;
            await Context.SaveChangesAsync();
            return person;
        }

        public async Task DeleteImage(
            long personId,
            Func<Person, Image?> imageGetter
        )
        {
            Person? person = await Context.Persons
                .AsTracking()
                .Include(x => x.ProfileImage)
                .Include(x => x.ClosetBackgroundImage)
                .Where(x => x.Id == personId)
                .SingleOrDefaultAsync();

            if (person == null)
                throw new ProblemDetailsException {
                    StatusCode = StatusCodes.Status404NotFound, Detail = Resources.Not_exists_person_
                };

            Image? exImage = imageGetter(person);
            if (exImage == null)
                return;

            await using IDbContextTransaction transaction = await Context.Database.BeginTransactionAsync();
            Context.Remove(exImage);
            await Context.SaveChangesAsync();
            await _cloudStorageService.DeleteAsync(exImage);
            await transaction.CommitAsync();
        }

        public async Task<Uri> ChangeImage(
            long personId,
            string keyPrefix ,
            Func<Person, Image?> imageGetter,
            Action<Person, Image> imageSetter,
            SupportedAspectRatio supportedAspectRatio,
            IFormFile file
        )
        {
            Image newImage;
            try
            {
                newImage = _cloudStorageService.CreateFile(
                    file,
                    $"{keyPrefix}/{personId}_{DateTimeOffset.UtcNow.UtcTicks}"
                );
            }
            catch (Exception e)
            {
                throw new ProblemDetailsException {
                    StatusCode = StatusCodes.Status400BadRequest,
                    Detail = string.Format(
                        Resources.Not_supported_image_format__0___,
                        Path.GetExtension(file.FileName)
                    )
                };
            }

            var imageAspectRatio = _imageService.getSupportedAspectRatio(newImage);
            if (imageAspectRatio != supportedAspectRatio)
            {
                _logger.LogError(
                    $"The aspect ratio of the attached photo is not supported.( images: {newImage.Width},{newImage.Height}, ratios: {imageAspectRatio} )");
                throw new ProblemDetailsException {
                    StatusCode = StatusCodes.Status400BadRequest,
                    Detail = Resources.The_aspect_ratio_of_the_attached_photo_is_not_supported_
                };
            }

            Person? person = await Context.Persons
                .AsTracking()
                .Include(x => x.ProfileImage)
                .Include(x => x.ClosetBackgroundImage)
                .Where(x => x.Id == personId)
                .SingleOrDefaultAsync();

            if (person == null)
                throw new ProblemDetailsException {
                    StatusCode = StatusCodes.Status404NotFound, Detail = Resources.Not_exists_person_
                };

            await using IDbContextTransaction transaction = await Context.Database.BeginTransactionAsync();
            Image? exImage = imageGetter(person);
            imageSetter(person, newImage);

            if (exImage != null)
                Context.Remove(exImage);

            await Context.SaveChangesAsync();

            await _cloudStorageService.UploadAsync(newImage, file.OpenReadStream());
            if (exImage != null)
                await _cloudStorageService.DeleteAsync(exImage);

            await transaction.CommitAsync();
            return _mapper.Map<Uri>(newImage);
        }

        public async Task<Person?> DeletePerson(long personId)
        {
            var person = await Context.Persons
                .Include(x => x.ProfileImage)
                .Include(x => x.ClosetBackgroundImage)
                .Include(x => x.Shots)
                .ThenInclude(x => x.Images)
                .Where(x => x.Id == personId)
                .FirstOrDefaultAsync();

            if (person == null)
                return null;


            await using var transaction = await Context.Database.BeginTransactionAsync();

            Context.Remove(person);

            var images = new List<Image>();
            if (person.ProfileImage != null)
                images.Add(person.ProfileImage);
            if (person.ClosetBackgroundImage != null)
                images.Add(person.ClosetBackgroundImage);
            images.AddRange(person.Shots.SelectMany(x => x.Images).ToList());
            Context.RemoveRange(images);
            Context.RemoveRange(person.Shots);

            await Context.SaveChangesAsync();

            await _cloudStorageService.DeleteAsync(images.ToArray());

            await transaction.CommitAsync();

            return person;
        }

        public async Task<Person> VerifyAndAdd(
            PersonPostArgs args,
            string appUuid
        )
        {
            if (args.Email.IsNullOrEmpty() && args.PhoneNumber.IsNullOrEmpty())
                throw new ProblemDetailsException {
                    StatusCode = StatusCodes.Status400BadRequest,
                    Detail = Resources.Either__Email__or__PhoneNumber__must_not_be_empty_
                };

            List<Person> persons = await FindAsync(x =>
                args.Name == x.Name
                || (args.Email != null && args.Email == x.Email)
                || (args.PhoneNumber != null && args.PhoneNumber == x.PhoneNumber)
            );

            var modelState = new ModelStateDictionary();

            persons.ForEach(person =>
            {
                if (args.Name.Equals(person.Name, StringComparison.InvariantCultureIgnoreCase))
                    modelState.AddModelError("name", Resources.Already_exists_name_);
                if (args.Email?.Equals(person.Email, StringComparison.InvariantCultureIgnoreCase) == true)
                    modelState.AddModelError("email", Resources.Already_exists_email_);
                if (args.PhoneNumber == person.PhoneNumber)
                    modelState.AddModelError("phoneNumber", Resources.Already_exists_phone_number_);
            });

            if (!modelState.IsValid)
                throw new ValidationProblemDetailsException(modelState) {StatusCode = StatusCodes.Status400BadRequest,};


            await using var transaction = await Context.Database.BeginTransactionAsync();

            var verification = await Context.Verifications
                .AsTracking()
                .FirstOrDefaultAsync(x =>
                    x.Id == args.VerificationId
                    && x.Purpose == VerificationPurpose.SignUp
                    && x.AppUuid == appUuid
                    && x.RequesterId == null
                    && x.VerifiedAt != null
                );

            if (verification == null
                || (verification.Method == VerificationMethod.Email ? args.Email : args.PhoneNumber) != verification.To
            )
                throw new ProblemDetailsException {
                    StatusCode = StatusCodes.Status403Forbidden, Detail = Resources.Not_verified_
                };

            if (verification.VerifiedAt?.Subtract(DateTimeOffset.UtcNow).TotalMinutes > 30)
                throw new ProblemDetailsException {
                    StatusCode = StatusCodes.Status400BadRequest,
                    Detail = Resources.Verification_has_expired__Please_verificate_again_
                };

            var entry = await Context.Persons.AddAsync(new Person {
                Name = args.Name,
                Email = args.Email,
                PhoneNumber = args.PhoneNumber,
                HashedPassword = _authenticationService.HashPassword(args.Password),
                Biography = "",
                IsEnabled = true
            });

            verification.RequesterId = entry.Entity.Id;

            await Context.SaveChangesAsync();
            await transaction.CommitAsync();
            return entry.Entity;
        }
    }
}