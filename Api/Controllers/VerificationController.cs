using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Api.Data.DataAnnotations;
using Api.Data.Models;
using Api.Data.Payload;
using Api.Errors;
using Api.Extension;
using Api.Properties;
using Api.Repositories;
using Api.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PhoneNumbers;

namespace Api.Controllers
{
    [ApiController, Authorize, Route("verification")]
    public class VerificationController : AbstractController
    {
        private readonly ILogger<VerificationController> _logger;
        private readonly VerificationService _verificationService;
        private readonly PersonRepository _personRepository;


        public VerificationController(
            ILogger<VerificationController> logger,
            VerificationService verificationService,
            PersonRepository personRepository
        )
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _verificationService = verificationService ?? throw new ArgumentNullException(nameof(verificationService));
            _personRepository = personRepository ?? throw new ArgumentNullException(nameof(personRepository));
        }

        [HttpPost("{purpose}/{method}")]
        [ProducesResponseType(typeof(PostRequestVerificationPayload), StatusCodes.Status200OK)]
        [ProducesDefaultResponseType]
        public async Task<ActionResult<PostRequestVerificationPayload>> PostRequestVerification(
            RouteVerificationPurpose purpose,
            VerificationMethod method,
            [FromHeader, Uuid] string appUuid
        )
        {
            var verification = await _verificationService.RequestVerification(
                purpose,
                method,
                appUuid,
                CurrentPersonId()
            );

            return Ok(new PostRequestVerificationPayload {
                VerificationId = verification.Id,
                Purpose = purpose,
                Method = method,
                CodeLength = verification.Code.Length
            });
        }

        [AllowAnonymous]
        [HttpPost("{purpose}/" + nameof(VerificationMethod.Sms) + "/{number}/{region}")]
        [ProducesResponseType(typeof(PostRequestAnonymousVerificationPayload), StatusCodes.Status200OK)]
        [ProducesDefaultResponseType]
        public async Task<ActionResult<PostRequestAnonymousVerificationPayload>> PostRequestAnonymousSmsVerification(
            RouteAnonymousVerificationPurpose purpose,
            [FromRoute] PhonePayload phonePayload,
            [FromHeader, Uuid] string appUuid
        )
        {
            var e164Number = phonePayload.Number.format(PhoneNumberFormat.E164, phonePayload.Region);

            CheckRequestValidate(purpose, VerificationMethod.Sms, e164Number);
            
            var verification = await _verificationService.RequestVerification(
                purpose,
                VerificationMethod.Sms,
                appUuid,
                e164Number
            );
            return Ok(new PostRequestAnonymousVerificationPayload {
                VerificationId = verification.Id,
                Purpose = purpose,
                Method = VerificationMethod.Sms,
                To = e164Number,
                CodeLength = verification.Code.Length
            });
        }

        [AllowAnonymous]
        [HttpPost("{purpose}/" + nameof(VerificationMethod.Email) + "/{email}")]
        [ProducesResponseType(typeof(PostRequestAnonymousVerificationPayload), StatusCodes.Status200OK)]
        [ProducesDefaultResponseType]
        public async Task<ActionResult<PostRequestAnonymousVerificationPayload>>
            PostRequestAnonymousEmailVerification(
                RouteAnonymousVerificationPurpose purpose,
                [EmailAddress] string email,
                [FromHeader, Uuid] string appUuid
            )
        {
            CheckRequestValidate(purpose, VerificationMethod.Email, email);

            var verification = await _verificationService.RequestVerification(
                purpose,
                VerificationMethod.Email,
                appUuid,
                email
            );

            return Ok(new PostRequestAnonymousVerificationPayload {
                VerificationId = verification.Id,
                Purpose = purpose,
                Method = VerificationMethod.Email,
                To = email,
                CodeLength = verification.Code.Length
            });
        }

        private void CheckRequestValidate(
            RouteAnonymousVerificationPurpose purpose,
            VerificationMethod method,
            string to
        )
        {
            switch (purpose)
            {
                case RouteAnonymousVerificationPurpose.SignUp:
                    if (_personRepository.Any(x =>
                        (method == VerificationMethod.Email ? x.Email : x.PhoneNumber) == to))
                        throw new ProblemDetailsException {
                            StatusCode = StatusCodes.Status400BadRequest,
                            Detail = string.Format(CultureInfo.CurrentCulture, Resources.The__0__is_already_used_, to)
                        };
                    break;
                case RouteAnonymousVerificationPurpose.ResetPassword:
                    if (!_personRepository.Any(x =>
                        (method == VerificationMethod.Email ? x.Email : x.PhoneNumber) == to))
                        throw new ProblemDetailsException {
                            StatusCode = StatusCodes.Status400BadRequest,
                            Detail = string.Format(CultureInfo.CurrentCulture, Resources.The__0__is_not_exist_, to)
                        };
                    break;
            }
        }

        [AllowAnonymous]
        [HttpGet("{verificationId}/newCode")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> GetRequestNewCode(
            [FromHeader, Uuid] string appUuid,
            long verificationId
        )
        {
            await _verificationService.RequestNewCode(appUuid, verificationId);
            return NoContent();
        }

        [AllowAnonymous]
        [HttpGet("{verificationId}/{verificationCode}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> GetVerifyCode(
            [FromHeader, Uuid] string appUuid,
            long verificationId,
            string verificationCode
        )
        {
            Verification? verification = await _verificationService.VerifyCode(
                appUuid,
                verificationId,
                verificationCode
            );
            if (verification == null)
                return Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    detail: Resources.Verification_Code_is_not_the_same_
                );

            RouteAnonymousVerificationPurpose? anonymousVerificationPurpose = null;
            try
            {
                anonymousVerificationPurpose = (RouteAnonymousVerificationPurpose)verification.Purpose;
            }
            catch (Exception e)
            {
                //Ignore
            }


            if (anonymousVerificationPurpose != null)
            {
                Person? person = verification.Method switch {
                    VerificationMethod.Sms => await _personRepository
                        .FindOneAsync(x => x.PhoneNumber == verification.To),
                    VerificationMethod.Email => await _personRepository
                        .FindOneAsync(x => x.Email == verification.To),
                    _ => null
                };

                ObjectResult? objectResult = (anonymousVerificationPurpose, person) switch {
                    var (x, y) when x == RouteAnonymousVerificationPurpose.SignUp && y != null =>
                    Problem(
                        statusCode: StatusCodes.Status400BadRequest,
                        detail: Resources.User_does_not_exist_
                    ),
                    (RouteAnonymousVerificationPurpose.ResetPassword, null) =>
                    Problem(
                        statusCode: StatusCodes.Status400BadRequest,
                        detail: Resources.User_does_not_exist_
                    ),
                    _ => null
                };

                if (objectResult != null)
                    return objectResult;
            }

            return NoContent();
        }
    }
}