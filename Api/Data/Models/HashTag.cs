using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Api.Data.DataAnnotations;
using Api.Data.Models.Relationships;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Data.Models
{
    [Table("hash_tag")]
    public class HashTag : Entity
    {
        [Column(TypeName = "citext"), Coordinate]
        public string Tag { get; set; } = default!;

        public virtual List<ShotHashTag> ShotHashTags { get; set; }
    }

    public class HashTagEntityTypeConfiguration : AbstractEntityTypeConfiguration<HashTag>
    {
        public override void Configure(EntityTypeBuilder<HashTag> builder)
        {
            base.Configure(builder);
            builder.HasIndex(x => x.Tag)
                .IsUnique();
        }
    }
}