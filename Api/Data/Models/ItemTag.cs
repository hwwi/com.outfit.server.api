using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Api.Data.DataAnnotations;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Data.Models
{
    [Table("item_tag")]
    public class ItemTag : Entity
    {
        [Coordinate] public float X { get; set; }

        [Coordinate] public float Y { get; set; }

        public long ProductId { get; set; }
        public virtual Product Product { get; set; }
        public long ImageId { get; set; }
        public virtual Image Image { get; set; }
    }

    public class ProductTagEntityTypeConfiguration : AbstractEntityTypeConfiguration<ItemTag>
    {
        public override void Configure(EntityTypeBuilder<ItemTag> builder)
        {
            base.Configure(builder);
            builder.HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x=>x.ProductId);
            builder.HasOne(x => x.Image)
                .WithMany(x => x.ItemTags)
                .HasForeignKey(x => x.ImageId);
        }
    }
}