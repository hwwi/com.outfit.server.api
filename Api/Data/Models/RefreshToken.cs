using System;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Data.Models
{
    [Table("refresh_token")]
    public class RefreshToken : Entity
    {
        public string Token { get; set; }
        public int ReissueCount { get; set; }
        public long PersonId { get; set; }
        public virtual Person Person { get; set; }
        public string AppUuid { get; set; }
        public DateTimeOffset ExpiredAt { get; set; }
        public bool IsExpired => DateTimeOffset.UtcNow >= ExpiredAt;
    }

    public class RefreshTokenEntityTypeConfiguration : AbstractEntityTypeConfiguration<RefreshToken>
    {
        public override void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            base.Configure(builder);
            builder.HasIndex(x => x.Token)
                .IsUnique();
            builder.HasIndex(x => new {
                    x.PersonId, x.AppUuid
                })
                .IsUnique();
            builder.HasOne(x => x.Person)
                .WithMany(x => x.RefreshTokens)
                .HasForeignKey(x => x.PersonId);
        }
    }
}