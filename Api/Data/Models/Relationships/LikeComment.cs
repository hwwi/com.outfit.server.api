using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Data.Models.Relationships
{
    [Table("like_comment")]
    public class LikeComment : Relationship
    {
        public long PersonId { get; set; }

        public virtual Person Person { get; set; }

        public long CommentId { get; set; }

        public virtual Comment Comment { get; set; }
    }

    public class LikeCommentEntityTypeConfiguration : RelationshipTypeConfiguration<LikeComment>
    {
        public override void Configure(EntityTypeBuilder<LikeComment> builder)
        {
            base.Configure(builder);
            builder.HasKey(x => new {x.PersonId, x.CommentId});
            builder.HasOne(x => x.Person)
                .WithMany(x => x.LikeComments)
                .HasForeignKey(x => x.PersonId);
            builder.HasOne(x => x.Comment)
                .WithMany(x => x.Likes)
                .HasForeignKey(x => x.CommentId);
        }
    }
}