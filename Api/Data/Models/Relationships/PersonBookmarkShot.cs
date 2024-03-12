using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Data.Models.Relationships
{
    [Table("person_bookmark_shot")]
    public class PersonBookmarkShot : Relationship
    {
        public long PersonId { get; set; }

        public virtual Person Person { get; set; }

        public long ShotId { get; set; }

        public virtual Shot Shot { get; set; }
    }

    public class PersonBookmarkShotEntityTypeConfiguration : RelationshipTypeConfiguration<PersonBookmarkShot>
    {
        public override void Configure(EntityTypeBuilder<PersonBookmarkShot> builder)
        {
            base.Configure(builder);
            builder.HasKey(x => new {x.PersonId, x.ShotId});
            builder.HasOne(x => x.Person)
                .WithMany(x => x.BookmarkShots)
                .HasForeignKey(x => x.PersonId);
            builder.HasOne(x => x.Shot)
                .WithMany(x => x.PersonBookmarks)
                .HasForeignKey(x => x.ShotId);
        }
    }
}