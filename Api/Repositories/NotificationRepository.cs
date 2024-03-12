using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Api.Data;
using Api.Data.Args;
using Api.Data.Dto;
using Api.Data.Models;
using Api.Extension;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace Api.Repositories
{
    public class NotificationRepository
    {
        private readonly OutfitDbContext _dbContext;
        private readonly IMapper _mapper;

        public NotificationRepository(OutfitDbContext dbContext, IMapper mapper)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }


        public async Task<Connection<NotificationDto>> FindConnectionDtoAsync(
            ConnectionArgs args,
            long viewerId,
            Expression<Func<Notification, bool>>? predicate)
        {
            IQueryable<Notification> queryable = _dbContext.Notifications
                    .Include(x => x.Shot)
                    .ThenInclude(x => x.Images)
                ;
            if (predicate != null)
                queryable = queryable.Where(predicate);

            return await queryable
                .ToConnectionAsync<Notification, NotificationDto>(
                    args,
                    _mapper,
                    new {viewerId}
                );
        }

        public async Task DeleteNotificationAsync(long personId, long notificationId)
        {
            var notification = await _dbContext.Notifications
                .Where(x => x.Id == notificationId && x.ConsumerId == personId)
                .FirstOrDefaultAsync();
            if (notification != null)
            {
                _dbContext.Remove(notification);
                await _dbContext.SaveChangesAsync();
            }
        }

        public async Task MergeTokenAsync(long? personId, string? currentToken, List<string>? expiredToken)
        {
            var needSaveChanges = false;
            if (currentToken != null)
            {
                PushToken? existPushToken = _dbContext.PushTokens
                    .AsTracking()
                    .FirstOrDefault(x => x.Token == currentToken);
                if (existPushToken == null)
                {
                    await _dbContext.PushTokens
                        .AddAsync(new PushToken {PersonId = personId, Token = currentToken});
                    needSaveChanges = true;
                }
                else if (existPushToken.PersonId != personId)
                {
                    existPushToken.PersonId = personId;
                    needSaveChanges = true;
                }
            }

            if (expiredToken != null && expiredToken.Count != 0)
            {
                var existExpiredFirebaseToken = await _dbContext.PushTokens
                    .Where(x => expiredToken.Contains(x.Token))
                    .ToListAsync();
                _dbContext.RemoveRange(existExpiredFirebaseToken);
                needSaveChanges = true;
            }

            if (needSaveChanges)
                await _dbContext.SaveChangesAsync();
        }

        public async Task DeleteTokenAsync(long? personId, string token)
        {
            var pushToken = await _dbContext.PushTokens
                .AsTracking()
                .Where(x =>
                    x.Token == token
                    && (personId == null || x.PersonId == personId)
                )
                .FirstOrDefaultAsync();
            if (pushToken?.PersonId != null)
            {
                pushToken.PersonId = null;
                await _dbContext.SaveChangesAsync();
            }
        }
    }
}