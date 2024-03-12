using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Api.Data.DataAnnotations;
using Api.Data.Models.Relationships;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Data.Models
{
    [Table("comment")]
    public class Comment : Entity
    {
        [CommentText] public string Text { get; set; }

        public long ShotId { get; set; }
        public virtual Shot Shot { get; set; }
        public long PersonId { get; set; }
        public virtual Person Person { get; set; }
        public virtual List<LikeComment> Likes { get; set; }
        public long? ParentId { get; set; }
        public virtual Comment? Parent { get; set; }
        public virtual List<Comment> Replies { get; set; }
    }

    public class CommentEntityTypeConfiguration : AbstractEntityTypeConfiguration<Comment>
    {
        public override void Configure(EntityTypeBuilder<Comment> builder)
        {
            base.Configure(builder);
            builder.HasOne(x => x.Shot)
                .WithMany(x => x.Comments)
                .HasForeignKey(x => x.ShotId);

            builder.HasOne(x => x.Person)
                .WithMany(x => x.Comments)
                .HasForeignKey(x => x.PersonId);

            builder.HasOne(x => x.Parent)
                .WithMany(x => x.Replies)
                .HasForeignKey(x => x.ParentId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}