using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Api.Data.Args;
using Api.Data.DataAnnotations;
using Api.Data.Models;
using Api.Data.Payload;
using Api.Extension;
using Api.Properties;
using Api.Repositories;
using Api.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PhoneNumbers;

namespace Api.Controllers
{
    [ApiController, Route("authentication")]
    public class AuthenticationController : AbstractController
    {
        private readonly ILogger<AuthenticationController> _logger;
        private readonly IStringLocalizer<AuthenticationController> _localizer;
        private readonly PersonRepository _personRepository;
        private readonly AuthenticationService _authenticationService;
        private readonly VerificationService _verificationService;

        private readonly EmailAddressAttribute _emailAddressAttribute;
        private readonly NotificationRepository _notificationRepository;

        public AuthenticationController(
            ILogger<AuthenticationController> logger,
            IStringLocalizer<AuthenticationController> localizer,
            PersonRepository personRepository,
            AuthenticationService authenticationService,
            VerificationService verificationService,
            NotificationRepository notificationRepository)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
            _personRepository = personRepository ?? throw new ArgumentNullException(nameof(personRepository));
            _authenticationService =
                authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
            _verificationService = verificationService ?? throw new ArgumentNullException(nameof(verificationService));
            _notificationRepository =
                notificationRepository ?? throw new ArgumentNullException(nameof(notificationRepository));

            _emailAddressAttribute = new EmailAddressAttribute();
        }

        [HttpPost("token")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesDefaultResponseType]
        public async Task<ActionResult<AuthPostTokenPayload>> PostAuthToken(
            [FromBody] AuthPostTokenArgs args,
            [FromHeader, Uuid] string appUuid
        )
        {
            return await _authenticationService.NewToken(appUuid, args);
        }

        [HttpPost("refreshToken")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesDefaultResponseType]
        public async Task<ActionResult<AuthPostRefreshTokenPayload>> PostAuthRefreshToken(
            [FromBody] AuthPostRefreshTokenArgs args,
            [FromHeader, Uuid] string appUuid
        )
        {
            try
            {
                return await _authenticationService.RefreshToken(appUuid, args);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "");
                return BadRequest();
            }
        }
    }
}