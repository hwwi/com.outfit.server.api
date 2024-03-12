using System;
using System.Threading.Tasks;
using Api.Configuration.ModelBinders;
using Api.Data.Args;
using Api.Data.Dto;
using Api.Data.Payload;
using Api.Repositories;
using Api.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Api.Controllers
{
    [ApiController, Route("shots"), Authorize]
    public class ShotsController : AbstractController
    {
        private readonly ILogger<ShotsController> _logger;
        private readonly ShotRepository _shotRepository;
        private readonly BrandRepository _brandRepository;
        private readonly ProductRepository _productRepository;
        private readonly CloudStorageService _cloudStorageService;
        private readonly PersonRepository _personRepository;

        public ShotsController(
            ILogger<ShotsController> logger,
            ShotRepository shotRepository,
            BrandRepository brandRepository,
            ProductRepository productRepository,
            CloudStorageService cloudStorageService,
            PersonRepository personRepository)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _shotRepository = shotRepository ?? throw new ArgumentNullException(nameof(shotRepository));
            _brandRepository = brandRepository ?? throw new ArgumentNullException(nameof(brandRepository));
            _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
            _cloudStorageService = cloudStorageService ?? throw new ArgumentNullException(nameof(cloudStorageService));
            _personRepository = personRepository ?? throw new ArgumentNullException(nameof(personRepository));
        }

        [HttpGet]
        public async Task<ActionResult<Connection<ShotDto>>> GetConnection(
            [FromQuery] long? personId,
            //TODO is need ValidationAttribute? 
            [FromQuery] string? brandCode,
            [FromQuery] string? productCode,
            [FromQuery] string? hashTag,
            [FromQuery] ConnectionArgs args
        )
        {
            long currentPersonId = CurrentPersonId();
            if (personId != null)
            {
                return await _shotRepository.FindConnectionDtoAsync(args, currentPersonId, x => x.PersonId == personId);
            }

            if (brandCode != null)
            {
                return await _shotRepository.FindConnectionDtoByBrandProductTagAsync(
                    args,
                    currentPersonId,
                    brandCode,
                    productCode);
            }

            if (hashTag != null)
                return await _shotRepository.FindConnectionDtoByHashTagAsync(
                    args,
                    currentPersonId,
                    hashTag
                );

            return await _shotRepository.FindConnectionDtoAsync(args, currentPersonId, null);
        }

        [HttpGet("private")]
        public async Task<ActionResult<Connection<ShotDto>>> GetPrivateConnection([FromQuery] ConnectionArgs args)
        {
            long currentPersonId = CurrentPersonId();

            return await _shotRepository.FindViewersPrivateConnectionDtoAsync(args, currentPersonId);
        }

        [HttpGet("bookmark")]
        public async Task<ActionResult<Connection<ShotDto>>> GetViewerBookmarkConnection(
            [FromQuery] ConnectionArgs args)
        {
            long currentPersonId = CurrentPersonId();

            return await _shotRepository.FindViewerBookmarkConnectionDtoAsync(args, currentPersonId);
        }

        [HttpGet("following")]
        public async Task<ActionResult<Connection<ShotDto>>> GetFollowingConnection([FromQuery] ConnectionArgs args)
        {
            long currentPersonId = CurrentPersonId();

            return await _shotRepository.FindViewerFollowingConnectionDtoAsync(args, currentPersonId);
        }

        [HttpGet("{shotId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ShotDto>> GetShot(long shotId)
        {
            ShotDto? shotDto = await _shotRepository.FindOneDtoAsync(CurrentPersonId(), shot => shot.Id == shotId);

            if (shotDto == null)
            {
                return NotFound("Not exist shot");
            }

            return Ok(shotDto);
        }

        /// <summary>
        /// Post a specific Shot.
        /// </summary>
        /// <param name="args">JsonString : ShotPostArgs</param>  
        /// <param name="files"></param>  
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ShotDto>> PostShot(
            [FromFormJsonString] ShotPostArgs args,
            IFormFileCollection files
        )
        {
            var shot = await _shotRepository.NewOneAsync(CurrentPersonId(), args, files);
            return CreatedAtAction(nameof(GetShot), new {shotId = shot.Id}, null);
        }

        [HttpDelete("{shotId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteShot(long shotId)
        {
            await _shotRepository.DeleteAsync(CurrentPersonId(), shotId);
            return NoContent();
        }

        [HttpPut("{shotId}/bookmark")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<ActionResult> PutBookmark(long shotId)
        {
            var currentPersonId = CurrentPersonId();
            await _shotRepository.SetBookmark(currentPersonId, shotId, true);
            return NoContent();
        }

        [HttpPut("{shotId}/like")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<LikeShotPayload>> PutShotLike(long shotId)
        {
            LikeShotPayload likeShotPayload = await _shotRepository.SetLike(CurrentPersonId(), shotId, true);
            return Ok(likeShotPayload);
        }

        [HttpPut("{shotId}/private")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<ActionResult> PutPrivate(long shotId)
        {
            await _shotRepository.SetPrivate(CurrentPersonId(), shotId, true);
            return NoContent();
        }

        [HttpDelete("{shotId}/bookmark")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<ActionResult> DeleteBookmark(long shotId)
        {
            await _shotRepository.SetBookmark(CurrentPersonId(), shotId, false);
            return NoContent();
        }

        [HttpDelete("{shotId}/like")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<LikeShotPayload>> DeleteShotLike(long shotId)
        {
            LikeShotPayload likeShotPayload = await _shotRepository.SetLike(CurrentPersonId(), shotId, false);
            return Ok(likeShotPayload);
        }

        [HttpDelete("{shotId}/private")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<ActionResult> DeletePrivate(long shotId)
        {
            await _shotRepository.SetPrivate(CurrentPersonId(), shotId, false);
            return NoContent();
        }
    }
}