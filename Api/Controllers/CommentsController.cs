using System;
using System.Threading.Tasks;
using Api.Data.Args;
using Api.Data.Dto;
using Api.Data.Models;
using Api.Data.Models.Relationships;
using Api.Data.Payload;
using Api.Properties;
using Api.Repositories;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Api.Controllers
{
    [ApiController, Route("shots/{shotId}/comments"), Authorize]
    public class CommentsController : AbstractController
    {
        private readonly ILogger<CommentsController> _logger;
        private readonly CommentRepository _commentRepository;
        private readonly IMapper _mapper;
        private readonly PersonRepository _personRepository;

        public CommentsController(
            ILogger<CommentsController> logger,
            CommentRepository commentRepository,
            IMapper mapper,
            PersonRepository personRepository)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _commentRepository = commentRepository ?? throw new ArgumentNullException(nameof(commentRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _personRepository = personRepository ?? throw new ArgumentNullException(nameof(personRepository));
        }

        [HttpGet]
        public async Task<ActionResult<Connection<CommentDto>>> GetCommentConnection(
            long shotId,
            [FromQuery] ConnectionArgs args)
        {
            return await _commentRepository.FindConnectionDtoByShotId(shotId, CurrentPersonId(), args);
        }

        [HttpGet("{commentId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<CommentDto>> GetComment(long shotId, long commentId)
        {
            CommentDto? commentDto =
                await _commentRepository.FindOneDtoAsync(
                    CurrentPersonId(),
                    x => x.Id == commentId && x.ShotId == shotId
                );

            if (commentDto == null)
            {
                return NotFound(Resources.Not_exist_comment_);
            }

            return Ok(commentDto);
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public async Task<ActionResult<CommentDto>> PostComment(long shotId, CommentPostArgs args)
        {
            var newCommentDto = await _commentRepository.NewOneAsync(CurrentPersonId(), shotId, null, args);
            return CreatedAtAction(nameof(GetComment), new {shotId, commentId = newCommentDto.Id}, newCommentDto);
        }

        [HttpPost("reply/{commentId}")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesDefaultResponseType]
        public async Task<ActionResult<CommentDto>> PostReplyComment(
            long shotId,
            long? commentId,
            CommentPostArgs args
        )
        {
            var newCommentDto = await _commentRepository.NewOneAsync(CurrentPersonId(), shotId, commentId, args);
            return CreatedAtAction(nameof(GetComment), new {shotId, commentId = newCommentDto.Id}, newCommentDto);
        }

        [HttpDelete("{commentId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteComment(long shotId, long commentId)
        {
            Comment? comment = await _commentRepository.DeleteAsync(CurrentPersonId(), shotId, commentId);

            if (comment == null)
            {
                return NotFound();
            }

            return NoContent();
        }


        [HttpPut("{commentId}/like")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<LikeCommentPayload>> PutCommentLike(long shotId, long commentId)
        {
            var payload = await _commentRepository.SetLike(CurrentPersonId(), shotId, commentId, true);
            return Ok(payload);
        }

        [HttpDelete("{commentId}/like")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<LikeCommentPayload>> DeleteCommentLike(long shotId, long commentId)
        {
            var payload = await _commentRepository.SetLike(CurrentPersonId(), shotId, commentId, false);
            return Ok(payload);
        }
    }
}