using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Api.Extension;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace Api.Data.Models
{
    [Table("image")]
    public class Image : Entity
    {
        public string Bucket { get; set; }
        public string Key { get; set; }
        public string? FileName { get; set; }
        public string? ContentType { get; set; }
        public string RawFormat { get; set; }
        public ExifOrientation Orientation { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public long Length { get; set; }
        public long? ShotId { get; set; }
        public virtual Shot? Shot { get; set; }
        public long? ProfilePersonId { get; set; }
        public virtual Person? ProfilePerson { get; set; }
        public long? ClosetBackgroundPersonId { get; set; }
        public virtual Person? ClosetBackgroundPerson { get; set; }
        public virtual List<ItemTag> ItemTags { get; set; }

        [NotMapped]
        public float AspectRatio {
            get {
                return Orientation.GetAspectRatio(Width, Height);
            }
        }
    }

    public class ImageEntityTypeConfiguration : AbstractEntityTypeConfiguration<Image>
    {
        public override void Configure(EntityTypeBuilder<Image> builder)
        {
            base.Configure(builder);
            builder.HasIndex(x => new {x.Bucket, x.Key})
                .IsUnique();

            builder.HasOne(x => x.Shot)
                .WithMany(x => x.Images)
                .HasForeignKey(x => x.ShotId)
                .OnDelete(DeleteBehavior.ClientNoAction);

            builder.HasOne(x => x.ProfilePerson)
                .WithOne(x => x.ProfileImage)
                .HasForeignKey<Image>(x => x.ProfilePersonId)
                .OnDelete(DeleteBehavior.ClientNoAction);

            builder.HasOne(x => x.ClosetBackgroundPerson)
                .WithOne(x => x.ClosetBackgroundImage)
                .HasForeignKey<Image>(x => x.ClosetBackgroundPersonId)
                .OnDelete(DeleteBehavior.ClientNoAction);
        }
    }
}