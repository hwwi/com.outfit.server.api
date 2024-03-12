using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Data.Models
{
    // ColumnAttribute.Order not yet supported. (https://github.com/aspnet/EntityFrameworkCore/issues/10059)
    public abstract class Entity : IEntity
    {
        [Key, Column(Order = 0)]
        public long Id { get; set; }

        [Column(Order = int.MaxValue)]
        public DateTimeOffset CreatedAt { get; set; }

        [Column(Order = int.MaxValue)]
        public DateTimeOffset? UpdatedAt { get; set; }
    }

    public abstract class AbstractEntityTypeConfiguration<T> : AbstractIAuditabilityTypeConfiguration<T>
        where T : class, IEntity
    {
        public override void Configure(EntityTypeBuilder<T> builder)
        {
            base.Configure(builder);
            builder.Property(x => x.Id)
                .UseHiLo($"{typeof(T).Name}HiLoSequence");
        }

    }
}