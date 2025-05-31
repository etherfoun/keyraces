using System;
using Xunit;
using keyraces.Core.Entities;

namespace keyraces.Core.Tests.Entities
{
    public class TypingSessionTests
    {
        [Fact]
        public void Constructor_ShouldInitializePropertiesCorrectly()
        {
            // Arrange
            var userId = 1;
            var textSnippetId = 101;
            var beforeCreation = DateTime.UtcNow;

            // Act
            var session = new TypingSession(userId, textSnippetId);
            var afterCreation = DateTime.UtcNow;

            // Assert
            Assert.Equal(userId, session.UserId);
            Assert.Equal(textSnippetId, session.TextSnippetId);
            Assert.True(session.StartTime >= beforeCreation && session.StartTime <= afterCreation.AddMilliseconds(100));
            // EndTime should be default(DateTime) or some indicator it's not set,
            // as it's set by Complete(). In C#, DateTime is a struct, so it defaults to 01/01/0001 00:00:00.
            Assert.Equal(default(DateTime), session.EndTime);
        }

        [Fact]
        public void Complete_ShouldSetEndTime()
        {
            // Arrange
            var session = new TypingSession(1, 101);
            var startTime = session.StartTime; // Capture start time
            System.Threading.Thread.Sleep(10); // Ensure EndTime will be measurably different
            var beforeCompletion = DateTime.UtcNow;

            // Act
            session.Complete();
            var afterCompletion = DateTime.UtcNow;

            // Assert
            Assert.True(session.EndTime >= beforeCompletion && session.EndTime <= afterCompletion.AddMilliseconds(100));
            Assert.True(session.EndTime > startTime); // EndTime should be after StartTime
        }

        [Fact]
        public void CompletedAt_ShouldReturnEndTime()
        {
            // Arrange
            var session = new TypingSession(1, 101);
            session.Complete();

            // Act
            var completedAt = session.CompletedAt;

            // Assert
            Assert.Equal(session.EndTime, completedAt);
        }

        [Fact]
        public void Constructor_Protected_CanBeUsedForEfCore()
        {
            // Arrange & Act
            var session = (TypingSession)System.Activator.CreateInstance(typeof(TypingSession), true)!;

            // Assert
            Assert.NotNull(session);
            Assert.Equal(0, session.UserId);
            Assert.Equal(0, session.TextSnippetId);
            Assert.Equal(default(DateTime), session.StartTime);
            Assert.Equal(default(DateTime), session.EndTime);
        }
    }
}
