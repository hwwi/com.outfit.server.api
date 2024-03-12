using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Api.Data.DataAnnotations;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Data.Models
{
    [Table("product")]
    public class Product : Entity
    {
        [Column(TypeName = "citext"), ProductCode]
        public string Code { get; set; }

        public long BrandId { get; set; }
        public virtual Brand Brand { get; set; }
    }

    public class ProductEntityTypeConfiguration : AbstractEntityTypeConfiguration<Product>
    {
        public override void Configure(EntityTypeBuilder<Product> builder)
        {
            base.Configure(builder);
            builder.HasIndex(x => new {x.BrandId, x.Code})
                .IsUnique();
            builder.HasOne(x => x.Brand)
                .WithMany(x => x.Products)
                .HasForeignKey(x => x.BrandId);
        }
    }
}