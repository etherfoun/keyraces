using System;
using Xunit;
using keyraces.Core.Entities;
using System.Collections.Generic; // Required for ICollection

namespace keyraces.Core.Tests.Entities
{
    public class UserProfileTests
    {
        [Fact]
        public void Constructor_ShouldInitializePropertiesCorrectly()
        {
            // Arrange
            var identityUserId = "auth|12345abc";
            var name = "John Doe";
            var beforeCreation = DateTime.UtcNow;

            // Act
            var userProfile = new UserProfile(identityUserId, name);
            var afterCreation = DateTime.UtcNow;

            // Assert
            Assert.Equal(identityUserId, userProfile.IdentityUserId);
            Assert.Equal(name, userProfile.Name);
            Assert.True(userProfile.CreatedAt >= beforeCreation && userProfile.CreatedAt <= afterCreation.AddMilliseconds(100));
            Assert.Equal(userProfile.CreatedAt, userProfile.UpdatedAt); // Initially CreatedAt and UpdatedAt are same
            Assert.Equal(userProfile.CreatedAt, userProfile.JoinedDate); // Initially CreatedAt and JoinedDate are same
            Assert.Equal(0, userProfile.AverageWPM);
            Assert.Equal(0, userProfile.AverageAccuracy);
            Assert.Equal(0, userProfile.TotalRaces);
            Assert.Equal(0, userProfile.TotalPractices);
            Assert.NotNull(userProfile.Achievements);
            Assert.Empty(userProfile.Achievements);
        }

        [Fact]
        public void Update_ValidName_ShouldUpdateNameAndUpdatedAt()
        {
            // Arrange
            var userProfile = new UserProfile("identity123", "Old Name");
            var oldUpdatedAt = userProfile.UpdatedAt;
            var newName = "New Name";
            System.Threading.Thread.Sleep(10); // Ensure UpdatedAt changes if system clock resolution is low

            // Act
            userProfile.Update(newName);

            // Assert
            Assert.Equal(newName, userProfile.Name);
            Assert.True(userProfile.UpdatedAt > oldUpdatedAt);
            Assert.Equal(userProfile.CreatedAt, userProfile.JoinedDate); // JoinedDate should not change
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void Update_InvalidName_ShouldThrowArgumentException(string invalidName)
        {
            // Arrange
            var userProfile = new UserProfile("identity123", "Test User");

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => userProfile.Update(invalidName));
            Assert.Equal("Name cannot be empty. (Parameter 'name')", exception.Message);
        }

        [Fact]
        public void UpdateStatistics_FirstTime_IsRace_ShouldSetAveragesAndCounts()
        {
            // Arrange
            var userProfile = new UserProfile("identity123", "Test User");
            double wpm = 50.0;
            double accuracy = 0.95;
            var oldUpdatedAt = userProfile.UpdatedAt;
            System.Threading.Thread.Sleep(10);

            // Act
            userProfile.UpdateStatistics(wpm, accuracy, true); // isRace = true

            // Assert
            Assert.Equal(wpm, userProfile.AverageWPM);
            Assert.Equal(accuracy, userProfile.AverageAccuracy);
            Assert.Equal(1, userProfile.TotalRaces);
            Assert.Equal(0, userProfile.TotalPractices);
            Assert.True(userProfile.UpdatedAt > oldUpdatedAt);
        }

        [Fact]
        public void UpdateStatistics_FirstTime_IsPractice_ShouldSetAveragesAndCounts()
        {
            // Arrange
            var userProfile = new UserProfile("identity123", "Test User");
            double wpm = 40.0;
            double accuracy = 0.90;
            var oldUpdatedAt = userProfile.UpdatedAt;
            System.Threading.Thread.Sleep(10);

            // Act
            userProfile.UpdateStatistics(wpm, accuracy, false); // isRace = false

            // Assert
            Assert.Equal(wpm, userProfile.AverageWPM);
            Assert.Equal(accuracy, userProfile.AverageAccuracy);
            Assert.Equal(0, userProfile.TotalRaces);
            Assert.Equal(1, userProfile.TotalPractices);
            Assert.True(userProfile.UpdatedAt > oldUpdatedAt);
        }

        [Fact]
        public void UpdateStatistics_SubsequentRace_ShouldUpdateAveragesAndRaceCount()
        {
            // Arrange
            var userProfile = new UserProfile("identity123", "Test User");
            userProfile.UpdateStatistics(50.0, 0.95, true); // First race (AvgWPM=50, AvgAcc=0.95, Races=1, Practices=0)

            double newWpm = 60.0;
            double newAccuracy = 0.98;
            var oldUpdatedAt = userProfile.UpdatedAt;
            System.Threading.Thread.Sleep(10);

            // Act
            userProfile.UpdateStatistics(newWpm, newAccuracy, true); // Second race

            // Assert
            // Total WPM = 50 + 60 = 110; Avg WPM = 110 / 2 = 55
            // Total Accuracy = 0.95 + 0.98 = 1.93; Avg Accuracy = 1.93 / 2 = 0.965
            Assert.Equal(55.0, userProfile.AverageWPM);
            Assert.Equal(0.965, userProfile.AverageAccuracy);
            Assert.Equal(2, userProfile.TotalRaces);
            Assert.Equal(0, userProfile.TotalPractices);
            Assert.True(userProfile.UpdatedAt > oldUpdatedAt);
        }

        [Fact]
        public void UpdateStatistics_SubsequentPractice_ShouldUpdateAveragesAndPracticeCount()
        {
            // Arrange
            var userProfile = new UserProfile("identity123", "Test User");
            userProfile.UpdateStatistics(50.0, 0.95, false); // First practice (AvgWPM=50, AvgAcc=0.95, Races=0, Practices=1)

            double newWpm = 60.0;
            double newAccuracy = 0.98;

            // Act
            userProfile.UpdateStatistics(newWpm, newAccuracy, false); // Second practice

            // Assert
            // Total WPM = 50 + 60 = 110; Avg WPM = 110 / 2 = 55
            // Total Accuracy = 0.95 + 0.98 = 1.93; Avg Accuracy = 1.93 / 2 = 0.965
            Assert.Equal(55.0, userProfile.AverageWPM);
            Assert.Equal(0.965, userProfile.AverageAccuracy);
            Assert.Equal(0, userProfile.TotalRaces);
            Assert.Equal(2, userProfile.TotalPractices);
        }

        [Fact]
        public void UpdateStatistics_MixRaceAndPractice_ShouldCalculateCorrectly()
        {
            // Arrange
            var userProfile = new UserProfile("identity123", "Test User");
            userProfile.UpdateStatistics(50.0, 0.90, true);  // Race 1: AvgWPM=50, AvgAcc=0.90, Races=1, Practices=0
            userProfile.UpdateStatistics(60.0, 0.92, false); // Practice 1: AvgWPM=(50+60)/2=55, AvgAcc=(0.90+0.92)/2=0.91, Races=1, Practices=1
            userProfile.UpdateStatistics(55.0, 0.95, true);  // Race 2: AvgWPM=(50+60+55)/3=55, AvgAcc=(0.90+0.92+0.95)/3=0.92333, Races=2, Practices=1

            // Expected:
            // Total WPM Sum = 50 + 60 + 55 = 165
            // Total Accuracy Sum = 0.90 + 0.92 + 0.95 = 2.77
            // Count = 3
            // Avg WPM = 165 / 3 = 55
            // Avg Accuracy = 2.77 / 3 = 0.92333...

            // Act
            // Statistics already updated in Arrange

            // Assert
            Assert.Equal(55.0, userProfile.AverageWPM, 5); // Precision up to 5 decimal places
            Assert.Equal(2.77 / 3.0, userProfile.AverageAccuracy, 5);
            Assert.Equal(2, userProfile.TotalRaces);
            Assert.Equal(1, userProfile.TotalPractices);
        }

        [Fact]
        public void Constructor_Protected_CanBeUsedForEfCore()
        {
            // Arrange & Act
            var profile = (UserProfile)System.Activator.CreateInstance(typeof(UserProfile), true)!;

            // Assert
            Assert.NotNull(profile);
            Assert.Equal(string.Empty, profile.IdentityUserId);
            Assert.Equal(string.Empty, profile.Name);
            Assert.Equal(default(DateTime), profile.CreatedAt);
            // Other properties would have their default values (0 for numbers, null for ICollection if not initialized)
            Assert.NotNull(profile.Achievements); // Assuming it's initialized in declaration or protected constructor
        }
    }
}
