using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Api.Data;
using Api.Data.Args;
using Api.Data.Dto;
using Api.Data.Models;
using Api.Data.Models.Relationships;
using Api.Data.Payload;
using Api.Errors;
using Api.Extension;
using Api.Properties;
using Api.Service;
using Api.Utils;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Castle.Core.Internal;
using FirebaseAdmin.Messaging;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Direction = Api.Data.Args.Direction;
using Notification = Api.Data.Models.Notification;

namespace Api.Repositories
{
    public class ShotRepository : AbstractEntityRepository<Shot>
    {
        private readonly ILogger<ShotRepository> _logger;
        private readonly IMapper _mapper;
        private readonly CloudStorageService _cloudStorageService;
        private readonly ImageService _imageService;
        private readonly CloudMessagingService _cloudMessagingService;
        private readonly CdnService _cdnService;

        public ShotRepository(
            ILogger<ShotRepository> logger,
            OutfitDbContext context,
            IMapper mapper,
            CloudStorageService cloudStorageService,
            ImageService imageService,
            CloudMessagingService cloudMessagingService,
            CdnService cdnService
        ) : base(context)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _cloudStorageService = cloudStorageService ?? throw new ArgumentNullException(nameof(cloudStorageService));
            _imageService = imageService ?? throw new ArgumentNullException(nameof(imageService));
            _cloudMessagingService =
                cloudMessagingService ?? throw new ArgumentNullException(nameof(cloudMessagingService));
            _cdnService = cdnService ?? throw new ArgumentNullException(nameof(cdnService));
        }

        public async Task<ShotDto?> FindOneDtoAsync(long viewerId, Expression<Func<Shot, bool>> predicate)
        {
            return await Context.Shots
                .Where(predicate)
                .Where(x => x.PersonId == viewerId || !x.IsPrivate)
                .ProjectTo<ShotDto>(_mapper.ConfigurationProvider, new {viewerId})
                .SingleOrDefaultAsync();
        }

        public async Task<Connection<ShotDto>> FindConnectionDtoAsync(
            ConnectionArgs args,
            long viewerId,
            Expression<Func<Shot, bool>>? predicate)
        {
            IQueryable<Shot> queryable = Context.Shots.Where(x => !x.IsPrivate);

            if (predicate != null)
                queryable = queryable.Where(predicate);

            return await queryable
                .ToConnectionAsync<Shot, ShotDto>(
                    args,
                    _mapper,
                    new {viewerId}
                );
        }

        public async Task<Connection<ShotDto>> FindViewersPrivateConnectionDtoAsync(
            ConnectionArgs args,
            long viewerId)
        {
            return await Context.Shots
                .Where(x => x.PersonId == viewerId && x.IsPrivate)
                .ToConnectionAsync<Shot, ShotDto>(
                    args,
                    _mapper,
                    new {viewerId}
                );
        }

        public async Task<Connection<ShotDto>> FindConnectionDtoByBrandProductTagAsync(
            ConnectionArgs args,
            long viewerId,
            string brandCode,
            string? productCode
        )
        {
            return await Context.Shots
                .Where(shot =>
                    !shot.IsPrivate
                    && shot.Images.Any(photo =>
                        photo.ItemTags.Any(tag =>
                            tag.Product.Brand.Code == brandCode
                            && (productCode == null || tag.Product.Code == productCode)
                        )
                    )
                )
                .ToConnectionAsync<Shot, ShotDto>(
                    args,
                    _mapper,
                    new {viewerId}
                );
        }

        public async Task<Connection<ShotDto>> FindConnectionDtoByHashTagAsync(
            ConnectionArgs args,
            long viewerId,
            string hashTag
        )
        {
            return await Context.Shots
                .Where(shot =>
                    !shot.IsPrivate
                    && shot.ShotHashTags.Any(y => y.HashTag.Tag == hashTag)
                )
                .ToConnectionAsync<Shot, ShotDto>(
                    args,
                    _mapper,
                    new {viewerId}
                );
        }

        public async Task<Connection<ShotDto>> FindViewerFollowingConnectionDtoAsync(
            ConnectionArgs args,
            long viewerId
        )
        {
            return await Context.Shots
                .Where(shot =>
                    !shot.IsPrivate
                    && Context.FollowPersons
                        .Where(x => x.FollowerId == viewerId)
                        .Select(x => x.Followed)
                        .Contains(shot.Person)
                )
                .ToConnectionAsync<Shot, ShotDto>(
                    args,
                    _mapper,
                    new {viewerId}
                );
        }

        public async Task<Connection<ShotDto>> FindViewerBookmarkConnectionDtoAsync(
            ConnectionArgs args,
            long viewerId
        )
        {
            var queryable = Context.PersonBookmarkShots
                .Where(x => x.PersonId == viewerId && !x.Shot.IsPrivate);

            if (args.Cursor != null)
            {
                queryable = args.Direction == Direction.After
                    ? queryable.Where(x => x.CreatedAt >= Context.PersonBookmarkShots
                        .First(x => x.ShotId == args.Cursor && x.PersonId == viewerId).CreatedAt)
                    : queryable.Where(x => x.CreatedAt <= Context.PersonBookmarkShots
                        .First(x => x.ShotId == args.Cursor && x.PersonId == viewerId).CreatedAt);
            }

            queryable = args.Direction == Direction.After
                ? queryable.OrderBy(x => x.CreatedAt)
                    .ThenBy(x => x.ShotId)
                : queryable.OrderByDescending(x => x.CreatedAt)
                    .ThenByDescending(x => x.ShotId);

            return await queryable
                .TakeConnectionAsync<PersonBookmarkShot, ShotDto>(
                    args,
                    _mapper,
                    new {viewerId}
                );
        }

        public async Task<Shot> NewOneAsync(
            long producerId,
            ShotPostArgs args,
            IFormFileCollection files
        )
        {
            if (files.Count == 0)
                throw new ProblemDetailsException {Detail = Resources.File_not_attached_};

            List<String> notSupportedImageFormats = files.Where(x => !x.IsSupportedImageFormat())
                .Select(x => Path.GetExtension(x.FileName))
                .Distinct()
                .ToList();

            if (notSupportedImageFormats.Count != 0)
                throw new ProblemDetailsException {
                    StatusCode = StatusCodes.Status400BadRequest,
                    Detail = string.Format(
                        Resources.Not_supported_image_format__0___,
                        string.Join(", ", notSupportedImageFormats)
                    )
                };


            if (args.TagListAndFileIndexList.Select(x => x.FileIndex).Any(x => files.Count <= x))
                throw new ProblemDetailsException {Detail = Resources.Tag_not_matched_then_files_};

            args.TagListAndFileIndexList
                .ForEach(x => x.ItemTags.ForEach(y =>
                {
                    y.Brand = y.Brand.ToUpperInvariant();
                    y.Product = y.Product.ToUpperInvariant();
                }));
            await using IDbContextTransaction transaction = await Context.Database.BeginTransactionAsync();

            Shot shot = new Shot {Caption = args.Caption, PersonId = producerId, Images = new List<Image>()};
            await Context.Shots.AddAsync(shot);

            List<Product> products;
            var flattened = args.TagListAndFileIndexList
                .SelectMany(x => x.ItemTags)
                .Select(x => new {x.Brand, x.Product})
                .Distinct()
                .ToList();

            if (!flattened.Any())
            {
                products = new List<Product>();
            }
            else
            {
                List<Brand> brands;
                var brandCodeToProductCodes = flattened
                    .GroupBy(x => x.Brand, x => x.Product)
                    .ToDictionary(
                        x => x.Key,
                        x => x.ToList()
                    );
                {
                    var brandCodes = brandCodeToProductCodes
                        .Select(x => x.Key)
                        .ToList();

                    var existBrands = await Context.Brands
                        .AsTracking()
                        .Where(x => brandCodes.Contains(x.Code.ToUpper()))
                        .ToListAsync();

                    var newBrands = brandCodes
                        .Where(x => existBrands.All(y => x != y.Code.ToUpper()))
                        .Select(x => new Brand {Code = x})
                        .ToList();

                    await Context.Brands.AddRangeAsync(newBrands);
                    brands = existBrands.Concat(newBrands).ToList();
                }

                var brandToProductCodes = brandCodeToProductCodes.ToDictionary(
                    x => brands.First(y => x.Key.Equals(y.Code, StringComparison.OrdinalIgnoreCase)),
                    x => x.Value
                );

                var existProducts = await Context.Products
                    .FromSqlRaw($@"
SELECT e.* 
  FROM {Context.Model.FindEntityType(typeof(Product)).GetTableName()} e
  JOIN ( values {string.Join(
                        ", ",
                        brandToProductCodes.SelectMany(x =>
                            x.Value.Select(y => $"({x.Key.Id}, '{y}')")
                        )
                    )}) 
    AS t(brand_id, code)
    ON e.brand_id = t.brand_id
   AND e.code     = t.code")
                    .Include(x => x.Brand)
                    .AsTracking()
                    .ToListAsync();

                var newProducts = brandToProductCodes
                    .SelectMany(pair =>
                        {
                            (Brand brand, List<string> productCodes) = pair;
                            return productCodes
                                .Where(productCode =>
                                    !existProducts.Any(existProduct =>
                                        existProduct.BrandId == brand.Id && existProduct.Code == productCode)
                                )
                                .Select(productCode => new Product {Brand = brand, Code = productCode});
                        }
                    )
                    .ToList();
                await Context.Products.AddRangeAsync(newProducts);
                products = existProducts.Concat(newProducts).ToList();
            }

            var images = files
                .Select((file, i) =>
                    _cloudStorageService.CreateFile(
                        file,
                        $"shots/{producerId}/{shot.Id}/{i}_{DateTimeOffset.UtcNow.UtcTicks}"
                    )
                )
                .ToList();

            var supportedAspectRatios = _imageService.getSupportedAspectRatio(
                images,
                new[] {
                    SupportedAspectRatio.Proportion4Over5, SupportedAspectRatio.Proportion1Over1,
                    SupportedAspectRatio.Proportion1Dot91Over1
                }
            );
            if (supportedAspectRatios.Count == 0
                || (images.Count != 1
                    && (supportedAspectRatios.Count != 1 ||
                        supportedAspectRatios[0] != SupportedAspectRatio.Proportion4Over5)
                )
            )
            {
                _logger.LogError(
                    $"The aspect ratio of the attached photo is not supported.( images: [{string.Join(",", images.Select(x => $"{x.Width},{x.Height}"))}], ratios: [{string.Join(",", supportedAspectRatios)}] )");
                throw new ProblemDetailsException {
                    StatusCode = StatusCodes.Status400BadRequest,
                    Detail = Resources.The_aspect_ratio_of_the_attached_photo_is_not_supported_
                };
            }

            for (var i = 0; i < images.Count; i++)
            {
                var image = images[i];
                image.ItemTags.AddRange(
                    args.TagListAndFileIndexList
                        .Where(x => x.FileIndex == i)
                        .SelectMany(x => x.ItemTags)
                        .Select(x =>
                            new ItemTag {
                                X = x.X,
                                Y = x.Y,
                                ProductId = products.First(product =>
                                    x.Brand.Equals(product.Brand.Code, StringComparison.OrdinalIgnoreCase)
                                    && x.Product.Equals(product.Code, StringComparison.OrdinalIgnoreCase)).Id
                            })
                );
                shot.Images.Add(image);
            }

            var hashTags = new List<HashTag>();
            var shotHashTags = new List<ShotHashTag>();
            foreach (Match match in Regexs.HashTag.Matches(shot.Caption))
            {
                var hashTag = new HashTag {Tag = match.Groups[Regexs.GroupHashName].Value};
                var shotHashTag = new ShotHashTag {Shot = shot, HashTag = hashTag};
                hashTags.Add(hashTag);
                shotHashTags.Add(shotHashTag);
            }

            await Context.HashTags.AddRangeAsync(hashTags);
            await Context.ShotHashTags.AddRangeAsync(shotHashTags);

            string? producerName = null;
            List<string> taggedPersonTokens;
            {
                var taggedPersonNames = Regexs.PersonTag.Matches(shot.Caption)
                    .Select(x => x.Groups[Regexs.GroupPersonName].Value.ToLowerInvariant())
                    .Distinct()
                    .ToList();
                if (taggedPersonNames.IsNullOrEmpty())
                {
                    taggedPersonTokens = new List<string>();
                }
                else
                {
                    var taggedPersons = await Context.Persons
                        .Where(x => x.Id != producerId && taggedPersonNames.Contains(x.Name))
                        .Select(x => new {x.Id, x.Name, Tokens = x.PushTokens.Select(y => y.Token).ToList()})
                        .ToListAsync();

                    producerName = taggedPersons
                        .Where(x => x.Id == producerId)
                        .Select(x => x.Name)
                        .FirstOrDefault();

                    taggedPersonTokens = taggedPersons.SelectMany(x => x.Tokens).ToList();

                    await Context.Notifications.AddRangeAsync(taggedPersons.Select(x => new Notification {
                            Type = NotificationType.ShotIncludePersonTag,
                            ShotId = shot.Id,
                            ProducerId = producerId,
                            ConsumerId = x.Id
                        })
                        .ToList());
                }
            }
            await Context.SaveChangesAsync();

            for (var i = 0; i < shot.Images.Count; i++)
                await _cloudStorageService.UploadAsync(shot.Images[i], files[i].OpenReadStream());

            await transaction.CommitAsync();

            var followerTokens = await Context.PushTokens
                .Where(x =>
                    Context.FollowPersons
                        .Where(followPerson => followPerson.FollowedId == producerId)
                        .Any(followPerson => followPerson.FollowerId == x.PersonId)
                )
                .Select(x => x.Token)
                .ToListAsync();

            //TODO 중복 토큰 한쪽만 보내줘야할까?
            if (followerTokens.Count != 0 || taggedPersonTokens.Count != 0)
            {
                if (producerName == null)
                    producerName = await Context.Persons
                        .Where(x => x.Id == producerId)
                        .Select(x => x.Name)
                        .FirstAsync();

                var previewImageUrl = _cdnService.getCdnPath(shot.Images.FirstOrDefault())?.ToString();
                if (followerTokens.Count != 0)
                    await _cloudMessagingService.SendMulticastAsync(
                        followerTokens,
                        NotificationType.ShotPosted,
                        shot.Id,
                        null,
                        new AndroidNotification {
                            TitleLocKey = "cloud_notification_title_shot_posted",
                            TitleLocArgs = new List<string> {producerName},
                            Body = shot.Caption.Truncate(100, "..."),
                            ImageUrl = previewImageUrl
                        }
                    );
                if (taggedPersonTokens.Count != 0)
                    await _cloudMessagingService.SendMulticastAsync(
                        taggedPersonTokens,
                        NotificationType.ShotIncludePersonTag,
                        shot.Id,
                        null,
                        new AndroidNotification {
                            TitleLocKey = "cloud_notification_title_shot_include_person_tag",
                            TitleLocArgs = new[] {producerName},
                            Body = shot.Caption.Truncate(100, "..."),
                            ImageUrl = previewImageUrl
                        }
                    );
            }

            return shot;
        }

        public async Task DeleteAsync(long personId, long shotId)
        {
            Shot? shot = await Context.Shots
                .Include(x => x.Images)
                .ThenInclude(x => x.ItemTags)
                .Where(x => x.Id == shotId && x.PersonId == personId)
                .SingleOrDefaultAsync();

            if (shot == null)
                throw new ProblemDetailsException {StatusCode = StatusCodes.Status404NotFound};


            await using var transaction = await Context.Database.BeginTransactionAsync();
            Context.Remove(shot);
            Context.RemoveRange(shot.Images);
            await Context.SaveChangesAsync();
            await _cloudStorageService.DeleteAsync(shot.Images.ToArray());
            await transaction.CommitAsync();
        }

        public async Task SetPrivate(long personId, long shotId, bool isPrivate)
        {
            var shot = await Context.Shots
                .AsTracking()
                .FirstOrDefaultAsync(x => x.Id == shotId);
            if (shot == null)
                throw new ProblemDetailsException {
                    StatusCode = StatusCodes.Status400BadRequest, Detail = Resources.Not_exists_shot_
                };
            if (shot.PersonId != personId)
                throw new ProblemDetailsException {StatusCode = StatusCodes.Status400BadRequest};

            if (shot.IsPrivate != isPrivate)
            {
                shot.IsPrivate = isPrivate;
                await Context.SaveChangesAsync();
            }
        }

        public async Task SetBookmark(long personId, long shotId, bool isBookmark)
        {
            var shot = await Context.Shots.FirstOrDefaultAsync(x => x.Id == shotId);

            if (shot == null)
                throw new ProblemDetailsException {
                    StatusCode = StatusCodes.Status400BadRequest, Detail = Resources.Not_exists_shot_
                };
            // Can't bookmark my shot.
            if (shot.PersonId == personId)
                throw new ProblemDetailsException {StatusCode = StatusCodes.Status400BadRequest};

            var personBookmarkShot = new PersonBookmarkShot {PersonId = personId, ShotId = shotId};
            try
            {
                if (isBookmark)
                    Context.Add(personBookmarkShot);
                else
                    Context.Remove(personBookmarkShot);
                await Context.SaveChangesAsync();
            }
            catch (DbUpdateException e)
            {
                // ignore, because already applied.
            }
        }

        public async Task<LikeShotPayload> SetLike(long producerId, long shotId, bool isLike)
        {
            var result = await Context.Shots
                .Where(x => x.Id == shotId)
                .Select(x => new {
                    LikesCount = x.Likes.Count, IsViewerLike = x.Likes.Any(y => y.PersonId == producerId)
                })
                .FirstAsync();
            if (result.IsViewerLike == isLike)
                return new LikeShotPayload {
                    ShotId = shotId, LikesCount = result.LikesCount, IsViewerLike = result.IsViewerLike
                };


            LikeShot likeShot = new LikeShot {PersonId = producerId, ShotId = shotId};

            if (isLike)
            {
                await Context.LikeShots.AddAsync(likeShot);
                var consumer = await Context.Persons
                    .Include(x => x.PushTokens)
                    .Where(x => x.Shots.Any(y => y.Id == shotId))
                    .Select(x => new {x.Id, Tokens = x.PushTokens.Select(y => y.Token).ToList()})
                    .FirstAsync();

                if (producerId == consumer.Id)
                    await Context.SaveChangesAsync();
                else
                {
                    await Context.Notifications.AddAsync(new Notification {
                        Type = NotificationType.ShotLiked,
                        ShotId = shotId,
                        ProducerId = producerId,
                        ConsumerId = consumer.Id
                    });

                    await Context.SaveChangesAsync();

                    if (consumer.Tokens.Count != 0)
                    {
                        var producerName = await Context.Persons
                            .Where(x => x.Id == producerId)
                            .Select(x => x.Name)
                            .FirstAsync();
                        try
                        {
                            await _cloudMessagingService.SendMulticastAsync(
                                consumer.Tokens,
                                NotificationType.ShotLiked,
                                shotId,
                                null,
                                new AndroidNotification {
                                    TitleLocKey = "cloud_notification_title_shot_liked",
                                    TitleLocArgs = new[] {producerName}
                                }
                            );
                        }
                        catch (Exception e)
                        {
                            _logger.LogError(e,
                                $"NotifyShotLiked fail -> personId: {producerId}, shotId: {shotId}, isLike: {isLike}");
                        }
                    }
                }
            }
            else
            {
                Context.Remove(likeShot);
                var notifications = await Context.Notifications
                    .Where(x =>
                        x.Type == NotificationType.ShotLiked
                        && x.ShotId == shotId
                        && x.ProducerId == producerId
                    )
                    .ToListAsync();
                Context.RemoveRange(notifications);
                await Context.SaveChangesAsync();
            }

            return new LikeShotPayload {
                ShotId = shotId, LikesCount = result.LikesCount + (isLike ? 1 : -1), IsViewerLike = isLike
            };
        }
    }
}