using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using keyraces.Core.Entities;

namespace keyraces.Infrastructure.Data
{
    public class AppDbContext : IdentityDbContext<IdentityUser, IdentityRole, string>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<UserProfile> UserProfiles { get; set; }
        public DbSet<TextSnippet> TextSnippets { get; set; }
        public DbSet<TypingSession> Sessions { get; set; }
        public DbSet<TypingStatistic> Statistics { get; set; }
        public DbSet<Competition> Competitions { get; set; }
        public DbSet<CompetitionParticipant> Participants { get; set; } 
        public DbSet<LeaderboardEntry> LeaderboardEntries { get; set; }
        public DbSet<Achievement> Achievements { get; set; }
        public DbSet<UserAchievement> UserAchievements { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<RefreshToken>(entity =>
            {
                entity.HasKey(rt => rt.Id);
                entity.Property(rt => rt.Token).IsRequired();
                entity.Property(rt => rt.JwtId).IsRequired();
                entity.Property(rt => rt.UserId).IsRequired();
                entity.Property(rt => rt.CreationDate).IsRequired();
                entity.Property(rt => rt.ExpiryDate).IsRequired();
            });

            builder.Entity<UserProfile>(b =>
            {
                b.ToTable("UserProfiles");
                b.HasKey(u => u.Id);
                b.HasIndex(u => u.IdentityUserId).IsUnique();

                b.HasOne<IdentityUser>()
                 .WithOne()
                 .HasForeignKey<UserProfile>(u => u.IdentityUserId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<CompetitionParticipant>()
                .HasKey(cp => new { cp.CompetitionId, cp.UserId });

            builder.Entity<UserAchievement>()
                .HasKey(ua => new { ua.UserId, ua.AchievementId });

            builder.Entity<CompetitionParticipant>()
                .HasOne(cp => cp.Competition)
                .WithMany(c => c.Participants)
                .HasForeignKey(cp => cp.CompetitionId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.Entity<CompetitionParticipant>()
                .HasOne<keyraces.Core.Entities.UserProfile>()
                .WithMany()
                .HasForeignKey(cp => cp.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<UserAchievement>()
                .HasOne(ua => ua.User)
                .WithMany(u => u.Achievements)
                .HasForeignKey(ua => ua.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.Entity<UserAchievement>()
                .HasOne(ua => ua.Achievement)
                .WithMany(a => a.AwardedTo)
                .HasForeignKey(ua => ua.AchievementId)
                .OnDelete(DeleteBehavior.Cascade);

        }
    }
}
