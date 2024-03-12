using System;
using System.Collections.Generic;
using Api.Data.DataAnnotations;

namespace Api.Data.Dto
{
    public class ShotDto : IIdentifiability
    {
        public long Id { get; set; }

        [ShotCaption]
        public string Caption { get; set; }

        public List<ImageDto> Images { get; set; }
        public int LikesCount { get; set; }
        public int CommentsCount { get; set; }
        public bool IsPrivate { get; set; }
        public bool IsViewerLike { get; set; }
        public bool IsViewerBookmark { get; set; }
        public PersonDto Person { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public DateTimeOffset? BookmarkedAt { get; set; }

        public override string ToString()
        {
            return $"{base.ToString()} => id: {Id}";
        }
    }
}