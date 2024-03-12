using System;
using System.Threading.Tasks;
using Api.Data.Args;
using Api.Data.DataAnnotations;
using Api.Data.Dto;
using Api.Data.Payload;
using Api.Properties;
using Api.Repositories;
using Api.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Api.Controllers
{
    [ApiController, Route("persons"), Authorize]
    public class PersonsController : AbstractController
    {
        private readonly ILogger<PersonsController> _logger;
        private readonly PersonRepository _personRepository;
        private readonly FollowPersonRepository _followPersonRepository;
        private readonly AuthenticationService _authenticationService;
        private readonly VerificationService _verificationService;

        public PersonsController(
            ILogger<PersonsController> logger,
            PersonRepository personService,
            FollowPersonRepository followPersonRepository,
            AuthenticationService authenticationService,
            VerificationService verificationService
        )
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _personRepository = personService ?? throw new ArgumentNullException(nameof(personService));
            _followPersonRepository =
                followPersonRepository ?? throw new ArgumentNullException(nameof(followPersonRepository));
            _authenticationService =
                authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
            _verificationService = verificationService ?? throw new ArgumentNullException(nameof(verificationService));
        }

        [HttpGet("{personId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PersonDto>> GetPerson(long personId)
        {
            PersonDto? personDto = await _personRepository.FindOneToDtoByAsync(x => x.Id == personId);

            if (personDto == null)
            {
                return NotFound(Resources.Not_exists_person_);
            }

            return Ok(personDto);
        }

        [HttpGet("detail/{personId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PersonDetailDto>> GetPersonDetail(long personId)
        {
            PersonDetailDto? dto =
                await _personRepository.FindOneToProfileDtoByAsync(CurrentPersonId(), x => x.Id == personId);

            if (dto == null)
            {
                return NotFound(Resources.Not_exists_person_);
            }

            return Ok(dto);
        }

        [HttpGet("detail")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PersonDetailDto>> GetPersonDetailByName([FromQuery] string name)
        {
            PersonDetailDto? dto =
                await _personRepository.FindOneToProfileDtoByAsync(CurrentPersonId(), x => x.Name == name);

            if (dto == null)
            {
                return NotFound(Resources.Not_exists_person_);
            }

            return Ok(dto);
        }

        [HttpGet("validate"), AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public IActionResult GetValidate(
            [FromQuery, PersonName] string? name
        )
        {
            return NoContent();
        }

        [HttpGet("follower/{personId}")]
        public async Task<ActionResult<Connection<PersonDto>>> GetConnectionByFollower(
            [FromRoute] long personId,
            [FromQuery] ConnectionArgs args,
            [FromQuery] string? keyword
        )
        {
            return await _personRepository.FindFollowerConnection(args, personId, keyword);
        }

        [HttpGet("following/{personId}")]
        public async Task<ActionResult<Connection<PersonDto>>> GetConnectionByFollowing(
            [FromRoute] long personId,
            [FromQuery] ConnectionArgs args,
            [FromQuery] string? keyword
        )
        {
            return await _personRepository.FindFollowingConnection(args, personId, keyword);
        }

        [HttpPost, AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public async Task<ActionResult<PersonDto>> PostPerson(
            [FromHeader, Uuid] string appUuid,
            PersonPostArgs args
        )
        {
            var entity = await _personRepository.VerifyAndAdd(args, appUuid);

            return CreatedAtAction(nameof(GetPerson), new {personId = entity.Id}, null);
        }

        [HttpPut("following/{personId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesDefaultResponseType]
        public async Task<ActionResult<FollowPersonPayload>> PutFollowing(long personId)
        {
            FollowPersonPayload followPersonPayload =
                await _followPersonRepository.SetFollowing(CurrentPersonId(), personId, true);
            return Ok(followPersonPayload);
        }

        [HttpDelete("following/{personId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<FollowPersonPayload>> DeleteFollowing(long personId)
        {
            FollowPersonPayload followPersonPayload =
                await _followPersonRepository.SetFollowing(CurrentPersonId(), personId, false);
            return Ok(followPersonPayload);
        }

        [HttpDelete("follower/{personId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<FollowPersonPayload>> DeleteFollower(long personId)
        {
            FollowPersonPayload followPersonPayload =
                await _followPersonRepository.SetFollowing(personId, CurrentPersonId(), false);
            return Ok(followPersonPayload);
        }
    }
}