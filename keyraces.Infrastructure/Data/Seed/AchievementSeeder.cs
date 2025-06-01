using keyraces.Core.Entities;
using keyraces.Core.Enums;
using keyraces.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace keyraces.Infrastructure.Data.Seed
{
    public static class AchievementSeeder
    {
        public static async Task SeedAsync(AppDbContext context, ILogger logger)
        {
            var achievementsToSeed = GetPredefinedAchievements();
            var existingAchievementKeys = await context.Achievements.Select(a => a.Key).ToListAsync();

            var newAchievements = achievementsToSeed.Where(sa => !existingAchievementKeys.Contains(sa.Key)).ToList();

            if (newAchievements.Any())
            {
                logger.LogInformation("Seeding {Count} new achievements...", newAchievements.Count);
                await context.Achievements.AddRangeAsync(newAchievements);
                await context.SaveChangesAsync();
                logger.LogInformation("{Count} new achievements seeded.", newAchievements.Count);
            }
            else
            {
                logger.LogInformation("No new achievements to seed.");
            }
        }

        private static IEnumerable<Achievement> GetPredefinedAchievements()
        {
            return new List<Achievement>
            {
                new Achievement(AchievementKey.FirstSessionCompleted, "First Steps", "Complete your first typing session.") { IconCssClass = "fas fa-shoe-prints" },
                new Achievement(AchievementKey.TenSessionsCompleted, "Persistent Typer", "Complete 10 typing sessions.") { IconCssClass = "fas fa-keyboard" },
                new Achievement(AchievementKey.FiftySessionsCompleted, "Keyboard Marathoner", "Complete 50 typing sessions.") { IconCssClass = "fas fa-running" },
                new Achievement(AchievementKey.SpeedDemon50WPM, "Speedy Fingers I", "Reach 50 WPM in a session.") { IconCssClass = "fas fa-tachometer-alt" },
                new Achievement(AchievementKey.SpeedDemon75WPM, "Speedy Fingers II", "Reach 75 WPM in a session.") { IconCssClass = "fas fa-fighter-jet" },
                new Achievement(AchievementKey.SpeedDemon100WPM, "Velocity Master", "Reach 100 WPM in a session.") { IconCssClass = "fas fa-rocket" },
                new Achievement(AchievementKey.AccuracyMaster98, "Sharp Shooter I", "Achieve 98% accuracy in a session.") { IconCssClass = "fas fa-bullseye" },
                new Achievement(AchievementKey.AccuracyMaster99, "Sharp Shooter II", "Achieve 99% accuracy in a session.") { IconCssClass = "fas fa-crosshairs" },
                new Achievement(AchievementKey.AccuracyMaster100, "Flawless Victory", "Achieve 100% accuracy in a session.") { IconCssClass = "fas fa-gem" },
            };
        }
    }
}
