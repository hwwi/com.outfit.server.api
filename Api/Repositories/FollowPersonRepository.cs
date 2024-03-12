using System;
using System.Linq;
using System.Threading.Tasks;
using Api.Data;
using Api.Data.Models.Relationships;
using Api.Data.Payload;
using Api.Errors;
using Api.Properties;
using Api.Service;
using FirebaseAdmin.Messaging;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Notification = Api.Data.Models.Notification;

namespace Api.Repositories
{
    public class FollowPersonRepository : AbstractRepository<FollowPerson>
    {
        private readonly ILogger<FollowPersonRepository> _logger;
        private readonly CloudMessagingService _cloudMessagingService;

        public FollowPersonRepository(
            OutfitDbContext context,
            ILogger<FollowPersonRepository> logger,
            CloudMessagingService cloudMessagingService
        ) : base(context)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cloudMessagingService =
                cloudMessagingService ?? throw new ArgumentNullException(nameof(cloudMessagingService));
        }

        public async Task<FollowPersonPayload> SetFollowing(long followerId, long followedId, bool isFollow)
        {
            if (followerId == followedId)
                throw new ProblemDetailsException {
                    StatusCode = StatusCodes.Status400BadRequest, Detail = Resources.Can_t_self_follow_
                };

            var persons = await Context.Persons
                .Where(x => x.Id == followerId || x.Id == followedId)
                .Select(x => new {
                    x.Id,
                    x.Name,
                    FollowingsCount = x.Followings.Count,
                    FollowersCount = x.Followers.Count,
                    IsFollowed = x.Followers.Any(y => y.FollowerId == followerId),
                    Tokens = x.PushTokens.Select(y => y.Token).ToList()
                })
                .ToListAsync();

            var follower = persons.FirstOrDefault(x => x.Id == followerId);
            var followed = persons.FirstOrDefault(x => x.Id == followedId);

            if (follower == null || followed == null)
                throw new ProblemDetailsException {
                    StatusCode = StatusCodes.Status400BadRequest, Detail = Resources.Not_exists_person_
                };

            if (followed.IsFollowed == isFollow)
                return new FollowPersonPayload {
                    FollowerId = follower.Id,
                    FollowerFollowingsCount = follower.FollowingsCount,
                    FollowedId = followed.Id,
                    FollowedFollowersCount = followed.FollowersCount,
                    IsFollow = isFollow
                };

            FollowPerson followPerson = new FollowPerson {FollowerId = follower.Id, FollowedId = followed.Id};
            if (isFollow)
            {
                await Context.FollowPersons.AddAsync(followPerson);
                await Context.Notifications.AddAsync(new Notification {
                    Type = NotificationType.Followed, ProducerId = follower.Id, ConsumerId = followed.Id
                });

                await Context.SaveChangesAsync();

                if (followed.Tokens.Count != 0)
                    try
                    {
                        await _cloudMessagingService.SendMulticastAsync(
                            followed.Tokens,
                            NotificationType.Followed,
                            null,
                            null,
                            new AndroidNotification {
                                TitleLocKey = "cloud_notification_title_followed",
                                TitleLocArgs = new[] {follower.Name},
                            }
                        );
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e,
                            $"NotifyFollowingStart fail -> ProducerId: {follower.Id}, ConsumerId: {followed.Id}, isFollow: {isFollow}");
                    }
            }
            else
            {
                Context.Remove(followPerson);
                var notifications = await Context.Notifications
                    .Where(x =>
                        x.Type == NotificationType.Followed
                        && x.ProducerId == follower.Id
                        && x.ConsumerId == followed.Id
                    )
                    .ToListAsync();
                Context.RemoveRange(notifications);
                await Context.SaveChangesAsync();
            }

            return new FollowPersonPayload {
                FollowerId = follower.Id,
                FollowerFollowingsCount = follower.FollowingsCount + (isFollow ? 1 : -1),
                FollowedId = followed.Id,
                FollowedFollowersCount = followed.FollowersCount + (isFollow ? 1 : -1),
                IsFollow = isFollow
            };
        }
    }
}