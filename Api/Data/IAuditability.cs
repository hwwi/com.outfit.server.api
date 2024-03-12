using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Data
{
    public interface IAuditability
    {
        DateTimeOffset CreatedAt { get; set; }
        DateTimeOffset? UpdatedAt { get; set; }
    }

    public abstract class AbstractIAuditabilityTypeConfiguration<T> : IEntityTypeConfiguration<T>
        where T : class, IAuditability
    {
        public virtual void Configure(EntityTypeBuilder<T> builder)
        {
            builder.Property(x => x.CreatedAt)
                .HasDefaultValueSql("current_timestamp");
        }
    }
}