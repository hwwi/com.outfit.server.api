using System;
using System.ComponentModel.DataAnnotations;
using Api.Data.DataAnnotations;

namespace Api.Data.Dto
{
    public class PersonDetailDto
    {
        public long Id { get; set; }

        [PersonName]
        public string Name { get; set; }

        [PersonBiography]
        public string Biography { get; set; }

        public Uri? ProfileImageUrl { get; set; }

        public Uri? ClosetBackgroundImageUrl { get; set; }

        public int ShotsCount { get; set; }

        public bool? IsViewerFollow { get; set; }

        public int FollowersCount { get; set; }

        public int FollowingsCount { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset? UpdatedAt { get; set; }
    }
}