using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Api.Data;
using Api.Data.Args;
using Api.Data.Dto;
using Api.Data.Models;
using Api.Data.Payload;
using Api.Extension;
using Api.Properties;
using Api.Repositories;
using Api.Service;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace Api.Controllers
{
    [ApiController, Route("account"), Authorize]
    public class AccountController : AbstractController
    {
        private readonly ILogger<AccountController> _logger;
        private readonly PersonRepository _personRepository;
        private readonly VerificationService _verificationService;
        private readonly AuthenticationService _authenticationService;
        private readonly NotificationRepository _notificationRepository;
        private readonly IMapper _mapper;

        public AccountController(
            ILogger<AccountController> logger,
            PersonRepository personRepository,
            AuthenticationService authenticationService,
            IMapper mapper,
            VerificationService verificationService,
            NotificationRepository notificationRepository
        )
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _personRepository = personRepository ?? throw new ArgumentNullException(nameof(personRepository));
            _authenticationService =
                authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _verificationService = verificationService ?? throw new ArgumentNullException(nameof(verificationService));
            _notificationRepository =
                notificationRepository ?? throw new ArgumentNullException(nameof(notificationRepository));
        }

        [HttpPut]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<AccountPutPayload>> PutAccount([FromBody] AccountPutArgs args)
        {
            var person = await _personRepository.Update(CurrentPersonId(), args.Biography);
            return Ok(new AccountPutPayload {PersonId = person.Id, Biography = person.Biography,});
        }


        [HttpPatch("name"), AllowAnonymous]
        [ProducesResponseType(typeof(AccountPatchNamePayload), StatusCodes.Status200OK)]
        public async Task<ActionResult<AccountPatchNamePayload>> PatchName([FromBody] AccountPatchNameArgs args)
        {
            var person = await _personRepository.ChangeName(CurrentPersonId(), args.Name);
            return Ok(new AccountPatchNamePayload {PersonId = person.Id, Name = person.Name});
        }


        [HttpPut("logout"), AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<ActionResult> PutLogout([FromBody] AccountPutLogoutArgs args)
        {
            await _notificationRepository.DeleteTokenAsync(CurrentPersonIdOrNull(), args.CurrentToken);
            return NoContent();
        }

        [HttpDelete]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteAccount()
        {
            Person? person = await _personRepository.DeletePerson(CurrentPersonId());

            if (person == null)
                return NotFound();

            return NoContent();
        }

        [HttpPatch("password")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> PatchAccountPassword([FromBody] AccountPasswordPatchArgs args)
        {
            Person? person = await _personRepository.FindOneByIdAsync(CurrentPersonId());
            if (person == null)
            {
                return NotFound(Resources.Not_exists_person_);
            }

            if (!_authenticationService.VerifyHashedPassword(person.HashedPassword, args.CurrentPassword))
            {
                return BadRequest(Resources.Current_password_is_incorrect_);
            }

            person.HashedPassword = _authenticationService.HashPassword(args.NewPassword);
            await _personRepository.UpdateAsync(person);

            return NoContent();
        }

        [AllowAnonymous]
        [HttpPatch("reset/password")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> PatchAccountResetPassword([FromBody] AccountResetPasswordPatchArgs args)
        {
            var verification = await _verificationService.FindOneByIdAsync(args.verificationId);
            if (verification == null
                || verification.VerifiedAt == null
                || verification.RequesterId != null)
                return Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    detail: Resources.The_verification_information_is_wrong_
                );

            Person? person = verification switch {
                {Method: VerificationMethod.Email} =>
                    await _personRepository.FindOneAsync(x => x.Email == verification.To),
                {Method: VerificationMethod.Sms} =>
                    await _personRepository.FindOneAsync(x => x.PhoneNumber == verification.To),
                _ => null
            };

            if (person == null)
                return Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    detail: Resources.User_not_found_
                );

            person.HashedPassword = _authenticationService.HashPassword(args.NewPassword);
            await _personRepository.UpdateAsync(person);

            return NoContent();
        }

        [HttpPatch("profileImage")]
        [ProducesResponseType(typeof(AccountPatchProfileImagePayload), StatusCodes.Status200OK)]
        public async Task<ActionResult<AccountPatchProfileImagePayload>> PatchProfileImage(IFormFile file)
        {
            long currentPersonId = CurrentPersonId();
            var uri = await _personRepository.ChangeImage(
                currentPersonId,
                "profiles",
                person => person.ProfileImage,
                (person, image) => person.ProfileImage = image,
                SupportedAspectRatio.Proportion1Over1,
                file
            );
            return Ok(new AccountPatchProfileImagePayload {PersonId = currentPersonId, ProfileImageUrl = uri});
        }

        [HttpPatch("closetBackgroundImage")]
        [ProducesResponseType(typeof(AccountPatchClosetBackgroundImagePayload), StatusCodes.Status200OK)]
        public async Task<ActionResult<AccountPatchClosetBackgroundImagePayload>> PatchClosetBackgroundImage(
            IFormFile file)
        {
            long currentPersonId = CurrentPersonId();
            var uri = await _personRepository.ChangeImage(
                currentPersonId,
                "closetBackgrounds",
                person => person.ClosetBackgroundImage,
                (person, image) => person.ClosetBackgroundImage = image,
                SupportedAspectRatio.Proportion5Over4,
                file
            );
            return Ok(new AccountPatchClosetBackgroundImagePayload {
                PersonId = currentPersonId, ClosetBackgroundImageUrl = uri
            });
        }

        [HttpDelete("profileImage")]
        [ProducesResponseType(typeof(AccountDeleteProfileImagePayload), StatusCodes.Status200OK)]
        public async Task<ActionResult<AccountDeleteProfileImagePayload>> DeleteProfileImage()
        {
            long currentPersonId = CurrentPersonId();
            await _personRepository.DeleteImage(
                currentPersonId,
                x => x.ProfileImage
            );
            return Ok(new AccountDeleteProfileImagePayload {PersonId = currentPersonId});
        }

        [HttpDelete("closetBackgroundImage")]
        [ProducesResponseType(typeof(AccountDeleteClosetBackgroundImagePayload), StatusCodes.Status200OK)]
        public async Task<ActionResult<AccountDeleteClosetBackgroundImagePayload>> DeleteClosetBackgroundImage()
        {
            long currentPersonId = CurrentPersonId();
            await _personRepository.DeleteImage(
                currentPersonId,
                x => x.ClosetBackgroundImage
            );
            return Ok(new AccountDeleteClosetBackgroundImagePayload {PersonId = currentPersonId});
        }
    }
}