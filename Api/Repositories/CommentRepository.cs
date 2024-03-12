using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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
using Microsoft.Extensions.Logging;
using Direction = Api.Data.Args.Direction;
using Notification = Api.Data.Models.Notification;

namespace Api.Repositories
{
    public class CommentRepository : AbstractEntityRepository<Comment>
    {
        private readonly ILogger<CommentRepository> _logger;
        private readonly CdnService _cdnService;
        private readonly CloudMessagingService _cloudMessagingService;
        private readonly IMapper _mapper;

        public CommentRepository(
            ILogger<CommentRepository> logger,
            OutfitDbContext context,
            CdnService cdnService,
            IMapper mapper,
            CloudMessagingService cloudMessagingService
        ) : base(context)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cdnService = cdnService ?? throw new ArgumentNullException(nameof(cdnService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _cloudMessagingService =
                cloudMessagingService ?? throw new ArgumentNullException(nameof(cloudMessagingService));
        }


        public async Task<CommentDto?> FindOneDtoAsync(
            long viewerId,
            Expression<Func<Comment, bool>> predicate
        )
        {
            return await Set
                .Where(predicate)
                .ProjectTo<CommentDto>(_mapper.ConfigurationProvider, new {viewerId})
                .SingleOrDefaultAsync();
        }

        public async Task<Comment?> FindOneRootParentAsync(long shotId, long commentId)
        {
            return await Set.FromSqlInterpolated($@"
SELECT * 
  FROM comment
WHERE id = (
    WITH RECURSIVE parents(id, parent_id) AS (
        SELECT id, parent_id
          FROM comment
         WHERE shot_id = {shotId}
           AND id = {commentId}
         UNION ALL
         SELECT t.id, t.parent_id
          FROM parents p
          JOIN comment t
            ON p.parent_id = t.id
    )
    SELECT id
      FROM parents
     WHERE parent_id is null
    )
")
                .SingleOrDefaultAsync();
        }


        public async Task<Connection<CommentDto>> FindConnectionDtoByShotId(
            long shotId,
            long viewerId,
            ConnectionArgs args
        )
        {
            return await
                Set.FromSqlRaw($@"
SELECT * 
  FROM ( WITH W_COMMENT AS (
         SELECT ROW_NUMBER() OVER ( partition by shot_id order by COALESCE(parent_id, id), id) AS row_number, *
           FROM comment
          WHERE shot_id = {{0}}
         ) 
         SELECT *
           FROM W_COMMENT
         {(args.Cursor != null ?
                            $@"
         WHERE row_number {(args.Direction == Direction.After ? ">=" : "<=")} (
                 SELECT row_number
                   FROM W_COMMENT
                  WHERE id = {{1}})
         "
                            : "")}
          ORDER BY row_number {(args.Direction == Direction.After ? "ASC" : "DESC")} ) AS ORDERED_COMMENT
",
                        shotId,
                        args.Cursor)
                    .TakeConnectionAsync<Comment, CommentDto>(
                        args,
                        _mapper,
                        new {viewerId}
                    );
        }

        public async Task<CommentDto> NewOneAsync(
            long producerId,
            long shotId,
            long? commentId,
            CommentPostArgs args)
        {
            long? rootParentId = null;
            if (commentId.HasValue)
            {
                Comment? rootParentComment = await FindOneRootParentAsync(shotId, commentId.Value);
                if (rootParentComment == null)
                    throw new ProblemDetailsException {
                        StatusCode = StatusCodes.Status400BadRequest, Detail = Resources.Can_t_find_parent_comment_
                    };
                rootParentId = rootParentComment.Id;
            }

            var comment = new Comment {
                Text = args.Text, ParentId = rootParentId, ShotId = shotId, PersonId = producerId
            };
            await Context.AddAsync(comment);

            List<string> commentedTokens;
            List<string> taggedPersonTokens;
            {
                var notifications = new List<Notification>();
                var commentedPersons = await Context.Persons
                    .Where(x =>
                        x.Id != producerId
                        && (
                            x.Shots.Any(y => y.Id == shotId)
                            || (commentId != null && commentId != rootParentId &&
                                x.Comments.Any(y => y.Id == commentId.Value))
                            || (rootParentId != null && x.Comments.Any(y => y.Id == rootParentId.Value))
                        )
                    )
                    .Select(x => new {x.Id, Tokens = x.PushTokens.Select(y => y.Token).ToList()})
                    .ToListAsync();

                commentedTokens = commentedPersons.SelectMany(x => x.Tokens).ToList();
                notifications.AddRange(commentedPersons.Select(x => new Notification {
                    Type = NotificationType.Commented,
                    ShotId = shotId,
                    CommentId = comment.Id,
                    ProducerId = producerId,
                    ConsumerId = x.Id
                }));


                var taggedPersonNames = Regexs.PersonTag.Matches(comment.Text)
                    .Select(x => x.Groups[Regexs.GroupPersonName].Value.ToLowerInvariant())
                    .Distinct()
                    .ToList();
                if (taggedPersonNames.IsNullOrEmpty())
                {
                    taggedPersonTokens = new List<string>();
                }
                else
                {
                    var commentedPersonIds = commentedPersons.Select(x => x.Id).ToList();
                    var taggedPerson = await Context.Persons
                        .Where(x =>
                            x.Id != producerId
                            && !commentedPersonIds.Contains(x.Id)
                            && taggedPersonNames.Contains(x.Name)
                        )
                        .Select(x => new {x.Id, Tokens = x.PushTokens.Select(y => y.Token).ToList()})
                        .ToListAsync();

                    taggedPersonTokens = taggedPerson.SelectMany(x => x.Tokens).ToList();

                    notifications.AddRange(taggedPerson.Select(x => new Notification {
                            Type = NotificationType.CommentIncludePersonTag,
                            ShotId = shotId,
                            CommentId = comment.Id,
                            ProducerId = producerId,
                            ConsumerId = x.Id
                        })
                    );
                }

                await Context.AddRangeAsync(notifications);
            }

            await Context.SaveChangesAsync();

            if (commentedTokens.Count != 0 || taggedPersonTokens.Count != 0)
            {
                var producerName = await Context.Persons
                    .Where(x => x.Id == producerId)
                    .Select(x => x.Name)
                    .FirstAsync();

                if (commentedTokens.Count != 0)
                    await _cloudMessagingService.SendMulticastAsync(
                        commentedTokens,
                        NotificationType.Commented,
                        shotId,
                        commentId,
                        new AndroidNotification {
                            TitleLocKey = "cloud_notification_title_commented",
                            TitleLocArgs = new[] {producerName},
                            Body = comment.Text.Truncate(100, "...")
                        }
                    );

                if (taggedPersonTokens.Count != 0)
                    await _cloudMessagingService.SendMulticastAsync(
                        taggedPersonTokens,
                        NotificationType.CommentIncludePersonTag,
                        shotId,
                        commentId,
                        new AndroidNotification {
                            TitleLocKey = "cloud_notification_title_comment_include_person_tag",
                            TitleLocArgs = new[] {producerName},
                            Body = comment.Text.Truncate(100, "..."),
                        }
                    );
            }

            return await FindOneDtoAsync(producerId, x => x.Id == comment.Id);
        }

        public async Task<Comment?> DeleteAsync(long personId, long shotId, long commentId)
        {
            var comment = await Context.Comments
                .Where(x => x.Id == commentId && x.ShotId == shotId && x.PersonId == personId)
                .FirstOrDefaultAsync();
            if (comment == null)
                return null;
            Context.Remove(comment);
            await Context.SaveChangesAsync();
            return comment;
        }

        public async Task<LikeCommentPayload> SetLike(long producerId, long shotId, long commentId, bool isLike)
        {
            var result = await Context.Comments
                .Where(x => x.Id == commentId)
                .Select(x => new {
                    LikesCount = x.Likes.Count, IsViewerLike = x.Likes.Any(y => y.PersonId == producerId)
                })
                .FirstAsync();
            if (result.IsViewerLike == isLike)
                return new LikeCommentPayload {
                    CommentId = commentId, LikesCount = result.LikesCount, IsViewerLike = result.IsViewerLike
                };

            var likeComment = new LikeComment {PersonId = producerId, CommentId = commentId};

            if (isLike)
            {
                await Context.LikeComments.AddAsync(likeComment);
                var consumer = await Context.Persons
                    .Include(x => x.PushTokens)
                    .Where(x => x.Comments.Any(y => y.Id == commentId))
                    .Select(x => new {x.Id, Tokens = x.PushTokens.Select(y => y.Token).ToList()})
                    .FirstAsync();

                if (producerId == consumer.Id)
                    await Context.SaveChangesAsync();
                else
                {
                    await Context.Notifications.AddAsync(new Notification {
                        Type = NotificationType.CommentLiked,
                        ShotId = shotId,
                        CommentId = commentId,
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
                                NotificationType.CommentLiked,
                                shotId,
                                commentId,
                                new AndroidNotification {
                                    TitleLocKey = "cloud_notification_title_comment_liked",
                                    TitleLocArgs = new[] {producerName},
                                }
                            );
                        }
                        catch (Exception e)
                        {
                            _logger.LogError(e,
                                $"NotifyCommentLiked fail -> personId: {producerId}, commentId: {commentId}, isLike: {isLike}");
                        }
                    }
                }
            }
            else
            {
                Context.Remove(likeComment);
                var notifications = await Context.Notifications
                    .Where(x =>
                        x.Type == NotificationType.CommentLiked
                        && x.ShotId == shotId
                        && x.CommentId == commentId
                        && x.ProducerId == producerId
                    )
                    .ToListAsync();
                Context.RemoveRange(notifications);
                await Context.SaveChangesAsync();
            }

            return new LikeCommentPayload {
                CommentId = commentId, LikesCount = result.LikesCount + (isLike ? 1 : -1), IsViewerLike = isLike
            };
        }
    }
}