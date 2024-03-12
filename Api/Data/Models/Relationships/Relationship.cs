using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Data.Models.Relationships
{
    public abstract class Relationship : IAuditability
    {
        [Column(Order = int.MaxValue)]
        public DateTimeOffset CreatedAt { get; set; }

        [Column(Order = int.MaxValue)]
        public DateTimeOffset? UpdatedAt { get; set; }
    }

    public abstract class RelationshipTypeConfiguration<T> : AbstractIAuditabilityTypeConfiguration<T>
        where T : Relationship
    {
    }
}