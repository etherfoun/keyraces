using Xunit;
using keyraces.Core.Entities;

namespace keyraces.Core.Tests.Entities
{
    public class TypingStatisticTests
    {
        [Fact]
        public void Constructor_ShouldInitializePropertiesCorrectly()
        {
            // Arrange
            var userId = 1;
            var sessionId = 101;
            var wpm = 65.5;
            var accuracy = 0.98;

            // Act
            var statistic = new TypingStatistic(userId, sessionId, wpm, accuracy);

            // Assert
            Assert.Equal(userId, statistic.UserId);
            Assert.Equal(sessionId, statistic.SessionId);
            Assert.Equal(wpm, statistic.WPM);
            Assert.Equal(accuracy, statistic.Accuracy);
        }

        [Fact]
        public void Update_ShouldUpdateWpmAndAccuracy()
        {
            // Arrange
            var statistic = new TypingStatistic(1, 1, 50.0, 0.95);
            double newWpm = 60.0;
            double newAccuracy = 0.98;

            // Act
            statistic.Update(newWpm, newAccuracy);

            // Assert
            Assert.Equal(newWpm, statistic.WPM);
            Assert.Equal(newAccuracy, statistic.Accuracy);
        }

        [Fact]
        public void Constructor_Protected_CanBeUsedForEfCore()
        {
            // Arrange & Act
            var statistic = (TypingStatistic)System.Activator.CreateInstance(typeof(TypingStatistic), true)!;

            // Assert
            Assert.NotNull(statistic);
            Assert.Equal(0, statistic.UserId);
            Assert.Equal(0, statistic.SessionId);
            Assert.Equal(0.0, statistic.WPM);
            Assert.Equal(0.0, statistic.Accuracy);
        }
    }
}
