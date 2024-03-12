using System;
using System.ComponentModel.DataAnnotations.Schema;
using Api.Service;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Data.Models
{
    [Table("verification")]
    public class Verification : Entity
    {
        public string AppUuid { get; set; }
        public long? RequesterId { get; set; }
        public virtual Person? Requester { get; set; }
        public VerificationPurpose Purpose { get; set; }
        public VerificationMethod Method { get; set; }
        public string To { get; set; }
        public string Code { get; set; }
        public string MessageId { get; set; }

        public int ReRequestCount { get; set; }

        // 인증 재요청시마다 업데이트, 최초 인증요청시간은 CreatedAt
        public DateTimeOffset RequestedAt { get; set; }
        public DateTimeOffset? VerifiedAt { get; set; }
    }

    public class VerificationEntityTypeConfiguration : AbstractEntityTypeConfiguration<Verification>
    {
        public override void Configure(EntityTypeBuilder<Verification> builder)
        {
            base.Configure(builder);
            builder.Property(x => x.RequestedAt)
                .HasDefaultValueSql("current_timestamp");
            builder.HasOne(x => x.Requester)
                .WithMany(x => x.Verifications)
                .HasForeignKey(x => x.RequesterId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}