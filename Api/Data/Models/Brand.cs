using Api.Data.DataAnnotations;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Api.Data.Models
{
    [Table("brand")]
    public class Brand : Entity
    {
        [Column(TypeName = "citext"), BrandCode]
        public string Code { get; set; }

        public virtual List<Product> Products { get; set; }
    }

    public class BrandEntityTypeConfiguration : AbstractEntityTypeConfiguration<Brand>
    {
        public override void Configure(EntityTypeBuilder<Brand> builder)
        {
            base.Configure(builder);
            builder.HasIndex("Code")
                .IsUnique();
        }
    }
}