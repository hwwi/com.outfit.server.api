using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Api.Data.DataAnnotations;
using Api.Data.Models.Relationships;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Namotion.Reflection;

namespace Api.Data.Models
{
    [Table("push_token")]
    public class PushToken : Entity
    {
        public string Token { get; set; }
        public long? PersonId { get; set; }
        public virtual Person? Person { get; set; }
    }

    public class PushTokenEntityTypeConfiguration : AbstractEntityTypeConfiguration<PushToken>
    {
        public override void Configure(EntityTypeBuilder<PushToken> builder)
        {
            base.Configure(builder);
            builder.HasIndex(x => x.Token)
                .IsUnique();
            builder.HasIndex(x => new {x.PersonId, x.Token})
                .IsUnique();
            builder.HasOne(x => x.Person)
                .WithMany(x => x.PushTokens)
                .HasForeignKey(x => x.PersonId);
        }
    }
}