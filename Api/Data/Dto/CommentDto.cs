using System;
using System.ComponentModel.DataAnnotations;
using Api.Data.DataAnnotations;
using Api.Data.Models;

namespace Api.Data.Dto
{
    public class CommentDto : IIdentifiability
    {
        [Required]
        public long Id { get; set; }

        [Required, CommentText]
        public string Text { get; set; }

        [Required]
        public int LikesCount { get; set; }

        [Required]
        public bool IsViewerLike { get; set; }

        [Required]
        public long ShotId { get; set; }

        [Required]
        public PersonDto Person { get; set; }

        [Required]
        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset? UpdatedAt { get; set; }

        [Required]
        public long? ParentId { get; set; }
    }
}