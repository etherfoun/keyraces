using Xunit;
using keyraces.Core.Entities;
using System.Collections.Generic; // Required for ICollection

namespace keyraces.Core.Tests.Entities
{
    public class AchievementTests
    {
        [Fact]
        public void Constructor_ShouldInitializePropertiesCorrectly()
        {
            // Arrange
            var name = "Speed Demon";
            var description = "Achieved WPM over 100";

            // Act
            var achievement = new Achievement(name, description);

            // Assert
            Assert.Equal(name, achievement.Name);
            Assert.Equal(description, achievement.Description);
            Assert.NotNull(achievement.AwardedTo);
            Assert.Empty(achievement.AwardedTo);
        }

        [Fact]
        public void Constructor_Protected_CanBeUsedForEfCore()
        {
            // Arrange & Act
            var achievement = (Achievement)System.Activator.CreateInstance(typeof(Achievement), true)!;

            // Assert
            Assert.NotNull(achievement);
            Assert.Equal(string.Empty, achievement.Name);
            Assert.Equal(string.Empty, achievement.Description);
            Assert.NotNull(achievement.AwardedTo);
        }
    }
}
