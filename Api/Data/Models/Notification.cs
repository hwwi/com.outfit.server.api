using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Data.Models
{
    /* 생성자와 소비자가 One to many 관계는 지원 안함(ex.. 신규 Shot 등록시 팔로워들에게 알림)
     * --------------------------------------------------------------------
     * | Type                     | Producer            | Consumer        |
     * -------------------------------------------------------------------- 
     * | ShotIncludePersonTag     | ShotRegister        | PersonTagName   |
     * | ShotLiked                | LikeShotRegister    | ShotRegister    |
     * | Commented                | CommentRegister     | ShotRegister    |
     * | CommentIncludePersonTag  | CommentRegister     | PersonTagName   |
     * | CommentLiked             | LikeCommentRegister | CommentRegister |
     * --------------------------------------------------------------------  
     */
    [Table("notification")]
    public class Notification : Entity
    {
        public NotificationType Type { get; set; }
        public long? ShotId { get; set; }
        public virtual Shot? Shot { get; set; }
        public long? CommentId { get; set; }
        public virtual Comment? Comment { get; set; }
        public long ProducerId { get; set; }
        public virtual Person Producer { get; set; }
        public long ConsumerId { get; set; }
        public virtual Person Consumer { get; set; }
    }


    public class NotificationEntityTypeConfiguration : AbstractEntityTypeConfiguration<Notification>
    {
        public override void Configure(EntityTypeBuilder<Notification> builder)
        {
            base.Configure(builder);
            builder.HasIndex(x =>
                new {
                    x.Type,
                    x.ShotId,
                    x.CommentId,
                    x.ConsumerId,
                    x.ProducerId
                }
            ).IsUnique();
            builder.HasOne(x => x.Shot)
                .WithMany()
                .HasForeignKey(x => x.ShotId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(x => x.Comment)
                .WithMany()
                .HasForeignKey(x => x.CommentId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(x => x.Producer)
                .WithMany()
                .HasForeignKey(x => x.ProducerId);
            builder.HasOne(x => x.Consumer)
                .WithMany()
                .HasForeignKey(x => x.ConsumerId);
        }
    }
}