using Xunit;
using keyraces.Core.Entities;

namespace keyraces.Core.Tests.Entities
{
    public class LeaderboardEntryTests
    {
        [Fact]
        public void Constructor_ShouldInitializePropertiesCorrectly()
        {
            // Arrange
            var competitionId = 1;
            var userId = 10;
            var rank = 1;
            var score = 1250.75;

            // Act
            var entry = new LeaderboardEntry(competitionId, userId, rank, score);

            // Assert
            Assert.Equal(competitionId, entry.CompetitionId);
            Assert.Equal(userId, entry.UserId);
            Assert.Equal(rank, entry.Rank);
            Assert.Equal(score, entry.Score);
        }

        [Fact]
        public void Constructor_Protected_CanBeUsedForEfCore()
        {
            // Arrange & Act
            var entry = (LeaderboardEntry)System.Activator.CreateInstance(typeof(LeaderboardEntry), true)!;

            // Assert
            Assert.NotNull(entry);
            Assert.Equal(0, entry.CompetitionId);
            Assert.Equal(0, entry.UserId);
            Assert.Equal(0, entry.Rank);
            Assert.Equal(0.0, entry.Score);
        }
    }
}
