using System.Collections.Generic;

namespace Api.Data.Args
{
    public class NotificationPostCloudMessagingTokenArgs
    {
        public string? CurrentToken { get; set; }
        public List<string>? ExpiredTokens { get; set; }
    }
}