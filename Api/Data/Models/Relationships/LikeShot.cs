using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Data.Models.Relationships
{
    [Table("like_shot")]
    public class LikeShot : Relationship
    {
        public long PersonId { get; set; } 

        public virtual Person Person { get; set; }

        public long ShotId { get; set; }

        public virtual Shot Shot { get; set; }
    }

    public class LikeShotEntityTypeConfiguration : RelationshipTypeConfiguration<LikeShot>
    {
        public override void Configure(EntityTypeBuilder<LikeShot> builder)
        {
            base.Configure(builder);
            builder.HasKey(x => new {x.PersonId, x.ShotId});
            builder.HasOne(x => x.Person)
                .WithMany(x => x.LikeShots)
                .HasForeignKey(x => x.PersonId);
            builder.HasOne(x => x.Shot)
                .WithMany(x => x.Likes)
                .HasForeignKey(x => x.ShotId);
        }
    }
}