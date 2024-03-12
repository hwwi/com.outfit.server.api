using System;

namespace Api.Data.Dto
{
    public class NotificationDto : IIdentifiability
    {
        public long Id { get; set; }
        public NotificationType Type { get; set; }

        public long? ShotId { get; set; }
        // Type in (ShotIncludePersonTag, ShotLiked) => not null
        public Uri? ShotPreviewImageUrl { get; set; }

        public long? CommentId { get; set; }
        // Type in (Commented, CommentIncludePersonTag, CommentLiked) => not null
        public string? CommentText { get; set; }
        
        public PersonDto Producer { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}