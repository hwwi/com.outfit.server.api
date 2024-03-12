using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Data.Models.Relationships
{
    [Table("shot_hash_tag")]
    public class ShotHashTag : Relationship
    {
        public long ShotId { get; set; }
        public virtual Shot Shot { get; set; }
        public long HashTagId { get; set; }
        public virtual HashTag HashTag { get; set; }
    }


    public class ShotHashTagEntityTypeConfiguration : RelationshipTypeConfiguration<ShotHashTag>
    {
        public override void Configure(EntityTypeBuilder<ShotHashTag> builder)
        {
            base.Configure(builder);
            builder.HasKey(x => new {x.ShotId, x.HashTagId});
            builder.HasOne(x => x.Shot)
                .WithMany(x => x.ShotHashTags)
                .HasForeignKey(x => x.ShotId);
            builder.HasOne(x => x.HashTag)
                .WithMany(x => x.ShotHashTags)
                .HasForeignKey(x => x.HashTagId);
        }
    }
}