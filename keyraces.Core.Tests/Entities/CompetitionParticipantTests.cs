using System;
using Xunit;
using keyraces.Core.Entities;

namespace keyraces.Core.Tests.Entities
{
    public class CompetitionParticipantTests
    {
        [Fact]
        public void Constructor_ShouldInitializePropertiesCorrectly()
        {
            // Arrange
            var competitionId = 1;
            var userId = 10;
            var beforeCreation = DateTime.UtcNow;

            // Act
            var participant = new CompetitionParticipant(competitionId, userId);

            // Assert
            Assert.Equal(competitionId, participant.CompetitionId);
            Assert.Equal(userId, participant.UserId);
            Assert.True(participant.JoinedAt >= beforeCreation);
            Assert.True(participant.JoinedAt <= DateTime.UtcNow.AddSeconds(1));
            Assert.Equal(ParticipantStatus.NotStarted, participant.Status);
            Assert.Null(participant.FinishedAt);
            Assert.Null(participant.WPM);
            Assert.Null(participant.ErrorCount);
        }

        [Fact]
        public void Begin_WhenNotStarted_ShouldSetStatusToTyping()
        {
            // Arrange
            var participant = new CompetitionParticipant(1, 1);
            Assert.Equal(ParticipantStatus.NotStarted, participant.Status);

            // Act
            participant.Begin();

            // Assert
            Assert.Equal(ParticipantStatus.Typing, participant.Status);
        }

        [Fact]
        public void Begin_WhenAlreadyTyping_ShouldRemainTyping()
        {
            // Arrange
            var participant = new CompetitionParticipant(1, 1);
            participant.Begin(); // First begin
            Assert.Equal(ParticipantStatus.Typing, participant.Status);

            // Act
            participant.Begin(); // Attempt to begin again

            // Assert
            Assert.Equal(ParticipantStatus.Typing, participant.Status);
        }

        [Fact]
        public void Begin_WhenCompleted_ShouldRemainCompleted()
        {
            // Arrange
            var participant = new CompetitionParticipant(1, 1);
            participant.Begin();
            participant.Complete(60, 2);
            Assert.Equal(ParticipantStatus.Completed, participant.Status);

            // Act
            participant.Begin(); // Attempt to begin after completion

            // Assert
            Assert.Equal(ParticipantStatus.Completed, participant.Status);
        }

        [Fact]
        public void Complete_WhenTyping_ShouldSetPropertiesCorrectly()
        {
            // Arrange
            var participant = new CompetitionParticipant(1, 1);
            participant.Begin(); // Participant must be typing to complete
            Assert.Equal(ParticipantStatus.Typing, participant.Status);

            double expectedWpm = 60.5;
            int expectedErrors = 5;
            var beforeCompletion = DateTime.UtcNow;

            // Act
            participant.Complete(expectedWpm, expectedErrors);

            // Assert
            Assert.Equal(ParticipantStatus.Completed, participant.Status);
            Assert.Equal(expectedWpm, participant.WPM);
            Assert.Equal(expectedErrors, participant.ErrorCount);
            Assert.NotNull(participant.FinishedAt);
            Assert.True(participant.FinishedAt >= beforeCompletion);
            Assert.True(participant.FinishedAt <= DateTime.UtcNow.AddSeconds(1));
        }

        [Fact]
        public void Complete_WhenNotStarted_ShouldNotChangeStatusOrProperties()
        {
            // Arrange
            var participant = new CompetitionParticipant(1, 1);
            Assert.Equal(ParticipantStatus.NotStarted, participant.Status);

            // Act
            participant.Complete(60, 2); // Attempt to complete without starting

            // Assert
            Assert.Equal(ParticipantStatus.NotStarted, participant.Status);
            Assert.Null(participant.WPM);
            Assert.Null(participant.ErrorCount);
            Assert.Null(participant.FinishedAt);
        }

        [Fact]
        public void Complete_WhenAlreadyCompleted_ShouldNotChangeProperties()
        {
            // Arrange
            var participant = new CompetitionParticipant(1, 1);
            participant.Begin();
            participant.Complete(60, 2);
            var firstWpm = participant.WPM;
            var firstErrors = participant.ErrorCount;
            var firstFinishedAt = participant.FinishedAt;
            Assert.Equal(ParticipantStatus.Completed, participant.Status);
            System.Threading.Thread.Sleep(10); // Ensure time moves

            // Act
            participant.Complete(70, 1); // Attempt to complete again with different values

            // Assert
            Assert.Equal(ParticipantStatus.Completed, participant.Status);
            Assert.Equal(firstWpm, participant.WPM);
            Assert.Equal(firstErrors, participant.ErrorCount);
            Assert.Equal(firstFinishedAt, participant.FinishedAt);
        }

        [Fact]
        public void Constructor_Protected_CanBeUsedForEfCore()
        {
            // Arrange & Act
            var participant = (CompetitionParticipant)System.Activator.CreateInstance(typeof(CompetitionParticipant), true)!;

            // Assert
            Assert.NotNull(participant);
            Assert.Equal(0, participant.CompetitionId);
            Assert.Equal(0, participant.UserId);
            Assert.Equal(default(DateTime), participant.JoinedAt); // Default if not initialized
            Assert.Equal(ParticipantStatus.NotStarted, participant.Status); // Default from enum or constructor
        }
    }
}
