using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Api.Configuration;
using Api.Data;
using Api.Data.Models;
using Api.Data.Models.Relationships;
using Api.Extension;
using Api.Repositories;
using AutoMapper;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Notification = Api.Data.Models.Notification;

namespace Api.Service
{
    public class CloudMessagingService : AbstractEntityRepository<Notification>
    {
        private readonly ILogger<CloudMessagingService> _logger;
        private readonly OutfitDbContext _context;
        private readonly IMapper _mapper;

        public CloudMessagingService(
            ILogger<CloudMessagingService> logger,
            OutfitDbContext context,
            IMapper mapper
        ) : base(context)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        public async Task SendMulticastAsync(
            List<string> tokens,
            NotificationType notificationType,
            long? shotId,
            long? commentId,
            AndroidNotification androidNotification
        )
        {
            var data = new Dictionary<string, string> {{"type", notificationType.ToString()}};

            if (shotId != null)
                data["shotId"] = $"{shotId}";
            if (commentId != null)
                data["commentId"] = $"{commentId}";

            var response = await FirebaseMessaging.DefaultInstance
                .SendMulticastAsync(new MulticastMessage {
                    Tokens = tokens, Data = data, Android = new AndroidConfig {Notification = androidNotification}
                });

            if (response.FailureCount != 0)
            {
                var unassignedTokens = new List<string>();
                var unregisteredTokens = new List<string>();
                var errorPerTokenDic = new Dictionary<MessagingErrorCode, List<string>>();

                for (var i = 0; i < response.Responses.Count; i++)
                {
                    var sendResponse = response.Responses[i];
                    if (sendResponse.IsSuccess)
                        continue;

                    var token = tokens[i];
                    if (sendResponse.Exception.MessagingErrorCode == null)
                    {
                        unassignedTokens.Add(token);
                        continue;
                    }

                    var errorCode = sendResponse.Exception.MessagingErrorCode.Value;

                    if (errorCode == MessagingErrorCode.Unregistered)
                    {
                        unregisteredTokens.Add(token);
                        continue;
                    }

                    List<string> list;
                    if (errorPerTokenDic.ContainsKey(errorCode))
                    {
                        list = errorPerTokenDic[errorCode];
                    }
                    else
                    {
                        list = new List<string>();
                        errorPerTokenDic[errorCode] = list;
                    }

                    list.Add(token);
                }

                if (unregisteredTokens.Count != 0)
                {
                    var unregisteredPushTokens = await Context.PushTokens
                        .Where(x => unregisteredTokens.Contains(x.Token))
                        .ToListAsync();
                    Context.PushTokens.RemoveRange(unregisteredPushTokens);
                    await Context.SaveChangesAsync();
                }

                if (errorPerTokenDic.Count != 0 || unassignedTokens.Count != 0)
                {
                    var sb = new StringBuilder($"List of tokens that caused failures{Environment.NewLine}");
                    foreach (var (key, value) in errorPerTokenDic)
                        sb.AppendLine($"{key} ({value.Count}): {string.Join(", ", value)}");

                    if (unassignedTokens.Count != 0)
                        sb.AppendLine(
                            $"unassignedTokens{unassignedTokens.Count}: {string.Join(", ", unassignedTokens)}");
                    _logger.LogError(sb.ToString());
                }
            }
        }
    }
}