using System;
using Xunit;
using keyraces.Core.Entities;
using System.Collections.Generic; // Required for ICollection

namespace keyraces.Core.Tests.Entities
{
    public class CompetitionTests
    {
        [Fact]
        public void Constructor_ShouldInitializePropertiesCorrectly()
        {
            // Arrange
            var title = "Weekly Typing Challenge";
            var textSnippetId = 101;
            var startTime = DateTime.UtcNow.AddDays(1);

            // Act
            var competition = new Competition(title, textSnippetId, startTime);

            // Assert
            Assert.Equal(title, competition.Title);
            Assert.Equal(textSnippetId, competition.TextSnippetId);
            Assert.Equal(startTime, competition.StartTime);
            Assert.Equal(CompetitionStatus.Scheduled, competition.Status);
            Assert.Null(competition.EndTime);
            Assert.NotNull(competition.Participants);
            Assert.Empty(competition.Participants);
        }

        [Fact]
        public void Start_WhenScheduled_ShouldSetStatusToInProgress()
        {
            // Arrange
            var competition = new Competition("Test Title", 1, DateTime.UtcNow.AddHours(1));
            Assert.Equal(CompetitionStatus.Scheduled, competition.Status); // Pre-condition

            // Act
            competition.Start();

            // Assert
            Assert.Equal(CompetitionStatus.InProgress, competition.Status);
        }

        [Fact]
        public void Start_WhenAlreadyInProgress_ShouldRemainInProgress()
        {
            // Arrange
            var competition = new Competition("Test Title", 1, DateTime.UtcNow.AddHours(1));
            competition.Start(); // First start
            Assert.Equal(CompetitionStatus.InProgress, competition.Status); // Pre-condition

            // Act
            competition.Start(); // Attempt to start again

            // Assert
            Assert.Equal(CompetitionStatus.InProgress, competition.Status); // Should not change
        }

        [Fact]
        public void Start_WhenFinished_ShouldRemainFinished()
        {
            // Arrange
            var competition = new Competition("Test Title", 1, DateTime.UtcNow.AddHours(-2));
            competition.Start();
            competition.Finish();
            Assert.Equal(CompetitionStatus.Finished, competition.Status); // Pre-condition

            // Act
            competition.Start(); // Attempt to start a finished competition

            // Assert
            Assert.Equal(CompetitionStatus.Finished, competition.Status); // Should not change
        }


        [Fact]
        public void Finish_WhenInProgress_ShouldSetStatusToFinishedAndEndTime()
        {
            // Arrange
            var competition = new Competition("Test Title", 1, DateTime.UtcNow.AddHours(-1));
            competition.Start(); // Competition must be in progress to be finished
            Assert.Equal(CompetitionStatus.InProgress, competition.Status); // Pre-condition
            var beforeFinishTime = DateTime.UtcNow;

            // Act
            competition.Finish();

            // Assert
            Assert.Equal(CompetitionStatus.Finished, competition.Status);
            Assert.NotNull(competition.EndTime);
            Assert.True(competition.EndTime >= beforeFinishTime);
            Assert.True(competition.EndTime <= DateTime.UtcNow.AddSeconds(1)); // Allow for slight delay
        }

        [Fact]
        public void Finish_WhenScheduled_ShouldRemainScheduledAndNotSetEndTime()
        {
            // Arrange
            var competition = new Competition("Test Title", 1, DateTime.UtcNow.AddHours(1));
            Assert.Equal(CompetitionStatus.Scheduled, competition.Status); // Pre-condition

            // Act
            competition.Finish(); // Attempt to finish a scheduled (not started) competition

            // Assert
            Assert.Equal(CompetitionStatus.Scheduled, competition.Status); // Status should not change
            Assert.Null(competition.EndTime); // EndTime should not be set
        }

        [Fact]
        public void Finish_WhenAlreadyFinished_ShouldRemainFinishedAndNotChangeEndTime()
        {
            // Arrange
            var competition = new Competition("Test Title", 1, DateTime.UtcNow.AddHours(-2));
            competition.Start();
            competition.Finish();
            var firstEndTime = competition.EndTime;
            Assert.Equal(CompetitionStatus.Finished, competition.Status); // Pre-condition
            System.Threading.Thread.Sleep(10); // Ensure time moves forward if Finish is called again

            // Act
            competition.Finish(); // Attempt to finish again

            // Assert
            Assert.Equal(CompetitionStatus.Finished, competition.Status);
            Assert.Equal(firstEndTime, competition.EndTime); // EndTime should not change
        }

        [Fact]
        public void Constructor_Protected_CanBeUsedForEfCore()
        {
            // Arrange & Act
            var competition = (Competition)System.Activator.CreateInstance(typeof(Competition), true)!;

            // Assert
            Assert.NotNull(competition);
            Assert.Equal(string.Empty, competition.Title);
            Assert.Equal(0, competition.TextSnippetId); // Default for int
            Assert.Equal(default(DateTime), competition.StartTime); // Default for DateTime
            Assert.Equal(CompetitionStatus.Scheduled, competition.Status); // Default from enum or constructor
            Assert.NotNull(competition.Participants);
        }
    }
}
