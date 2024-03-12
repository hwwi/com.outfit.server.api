using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Api.Data.DataAnnotations;
using Api.Data.Models.Relationships;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Data.Models
{
    [Table("person")]
    public class Person : Entity
    {
        [Column(TypeName = "citext"), MaxLength(20)]
        public string Name { get; set; }

        [Column(TypeName = "citext"), EmailAddress]
        public string? Email { get; set; }

        [Phone] public string? PhoneNumber { get; set; }

        public string HashedPassword { get; set; }

        [PersonBiography] public string Biography { get; set; }
        
        public virtual Image? ProfileImage { get; set; }
        
        public virtual Image? ClosetBackgroundImage { get; set; }

        public bool IsEnabled { get; set; }

        public virtual List<Shot> Shots { get; set; }
        public virtual List<Comment> Comments { get; set; }

        public virtual List<FollowPerson> Followers { get; set; }
        public virtual List<FollowPerson> Followings { get; set; }
        public virtual List<PersonBookmarkShot> BookmarkShots { get; set; }
        public virtual List<LikeShot> LikeShots { get; set; }
        public virtual List<LikeComment> LikeComments { get; set; }
        
        public virtual List<PushToken> PushTokens { get; set; }
        public virtual List<RefreshToken> RefreshTokens { get; set; }
        
        public virtual List<Verification> Verifications { get; set; }

        public DateTimeOffset? LastNameUpdatedAt { get; set; }
    }

    public class PersonEntityTypeConfiguration : AbstractEntityTypeConfiguration<Person>
    {
        public override void Configure(EntityTypeBuilder<Person> builder)
        {
            base.Configure(builder);
            builder.HasIndex(x => x.Name)
                .IsUnique();
            builder.HasIndex(x => x.Email)
                .IsUnique();
            builder.HasIndex(x => x.PhoneNumber)
                .IsUnique();
        }
    }
}