using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Api.Data.DataAnnotations;
using Api.Data.Models.Relationships;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Data.Models
{
    [Table("shot")]
    public class Shot : Entity
    {
        [ShotCaption]
        public string Caption { get; set; }
        public bool IsPrivate { get; set; }

        public virtual List<Image> Images { get; set; }
        public virtual List<Comment> Comments { get; set; }
        public virtual List<LikeShot> Likes { get; set; }
        public virtual List<PersonBookmarkShot> PersonBookmarks { get; set; }

        public long PersonId { get; set; }
        public virtual Person Person { get; set; }
        public virtual List<ShotHashTag> ShotHashTags { get; set; }
    }

    public class ShotEntityTypeConfiguration : AbstractEntityTypeConfiguration<Shot>
    {
        public override void Configure(EntityTypeBuilder<Shot> builder)
        {
            base.Configure(builder);
            builder.HasOne(x => x.Person)
                .WithMany(x => x.Shots)
                .HasForeignKey(x => x.PersonId);
        }
    }
}