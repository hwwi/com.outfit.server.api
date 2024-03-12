using System;
using System.Threading.Tasks;
using Api.Data.Args;
using Api.Data.Dto;
using Api.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [ApiController, Route("notification")]
    public class NotificationController : AbstractController
    {
        private readonly NotificationRepository _notificationRepository;

        public NotificationController(NotificationRepository notificationRepository)
        {
            _notificationRepository =
                notificationRepository ?? throw new ArgumentNullException(nameof(notificationRepository));
        }


        [HttpGet]
        public async Task<ActionResult<Connection<NotificationDto>>> GetNotificationConnection(
            [FromQuery] ConnectionArgs args
        )
        {
            long currentPersonId = CurrentPersonId();
            return await _notificationRepository.FindConnectionDtoAsync(
                args,
                currentPersonId,
                x => x.ConsumerId == currentPersonId
            );
        }

        [HttpDelete("{notificationId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<ActionResult> DeleteNotification(
            long notificationId
        )
        {
            await _notificationRepository.DeleteNotificationAsync(CurrentPersonId(), notificationId);
            return NoContent();
        }

        [HttpPost("cloudMessagingToken"), AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<ActionResult> PostCloudMessagingToken(
            NotificationPostCloudMessagingTokenArgs args
        )
        {
            await _notificationRepository.MergeTokenAsync(
                CurrentPersonIdOrNull(),
                args.CurrentToken,
                args.ExpiredTokens
            );
            return NoContent();
        }
    }
}