using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Data.Models.Relationships
{
    [Table("follow_person")]
    public class FollowPerson : Relationship
    {
        public long FollowerId { get; set; }
        public virtual Person Follower { get; set; }
        public long FollowedId { get; set; }
        public virtual Person Followed { get; set; }
    }

    public class FollowPersonRelationshipTypeConfiguration : RelationshipTypeConfiguration<FollowPerson>
    {
        public override void Configure(EntityTypeBuilder<FollowPerson> builder)
        {
            base.Configure(builder);
            builder.HasKey(x => new {x.FollowerId, x.FollowedId});
            builder.HasOne(x => x.Follower)
                .WithMany(x => x.Followings)
                .HasForeignKey(x => x.FollowerId);
            builder.HasOne(x => x.Followed)
                .WithMany(x => x.Followers)
                .HasForeignKey(x => x.FollowedId);
        }
    }
}