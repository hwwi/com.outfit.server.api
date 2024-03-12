using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Api.Data.Models;
using Api.Data.Models.Relationships;
using Api.Extension;
using Api.Service;
using Microsoft.EntityFrameworkCore;

namespace Api.Data
{
    public class OutfitDbContext : DbContext
    {
        public DbSet<Person> Persons { get; set; }
        public DbSet<Verification> Verifications { get; set; }
        public DbSet<FollowPerson> FollowPersons { get; set; }
        public DbSet<PushToken> PushTokens { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<Shot> Shots { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Image> Images { get; set; }
        public DbSet<HashTag> HashTags { get; set; }
        public DbSet<ShotHashTag> ShotHashTags { get; set; }
        public DbSet<ItemTag> ItemTags { get; set; }
        public DbSet<Brand> Brands { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<LikeShot> LikeShots { get; set; }
        public DbSet<LikeComment> LikeComments { get; set; }
        public DbSet<PersonBookmarkShot> PersonBookmarkShots { get; set; }
        public DbSet<Notification> Notifications { get; set; }


        public OutfitDbContext(DbContextOptions options) : base(options)
        {
            ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        }

        //https://github.com/aspnet/EntityFrameworkCore/issues/4050 [Support an [Index] attribute]
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
            modelBuilder.HasPostgresEnum<VerificationMethod>();
            modelBuilder.HasPostgresEnum<VerificationPurpose>();
            modelBuilder.HasPostgresEnum<ExifOrientation>();
            modelBuilder.HasPostgresEnum<NotificationType>();
            modelBuilder.HasPostgresExtension("citext");
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            UpdateTrackedEntriesUpdateAt();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess,
            CancellationToken cancellationToken = default)
        {
            UpdateTrackedEntriesUpdateAt();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        private void UpdateTrackedEntriesUpdateAt()
        {
            foreach (var entry in ChangeTracker.Entries())
            {
                if (entry.State == EntityState.Modified && entry.Entity is Entity entity)
                    entity.UpdatedAt = DateTimeOffset.Now;
            }
        }
    }
}