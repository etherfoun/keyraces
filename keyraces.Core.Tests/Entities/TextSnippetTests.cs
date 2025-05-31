using System;
using Xunit;
using keyraces.Core.Entities;

namespace keyraces.Core.Tests.Entities
{
    public class TextSnippetTests
    {
        [Fact]
        public void DefaultConstructor_ShouldInitializePropertiesWithDefaults()
        {
            // Arrange & Act
            var snippet = new TextSnippet();
            var creationTimeLowerBound = DateTime.UtcNow.AddSeconds(-1);
            var creationTimeUpperBound = DateTime.UtcNow.AddSeconds(1);


            // Assert
            Assert.Null(snippet.Title); // Default for string if not initialized
            Assert.Null(snippet.Content); // Default for string if not initialized
            Assert.Equal("medium", snippet.Difficulty);
            Assert.False(snippet.IsGenerated);
            Assert.True(snippet.CreatedAt >= creationTimeLowerBound && snippet.CreatedAt <= creationTimeUpperBound);
            Assert.Equal("ru", snippet.Language);
        }

        [Fact]
        public void Constructor_WithContentDifficultyLanguage_ShouldInitializeCorrectly()
        {
            // Arrange
            var content = "This is a test snippet.";
            var difficulty = "hard";
            var language = "en";
            var beforeCreation = DateTime.UtcNow;

            // Act
            var snippet = new TextSnippet(content, difficulty, language);
            var afterCreation = DateTime.UtcNow;

            // Assert
            Assert.Equal(content, snippet.Content);
            Assert.Equal(difficulty, snippet.Difficulty);
            Assert.Equal(language, snippet.Language);
            Assert.StartsWith("Text ", snippet.Title); // Title is auto-generated
            Assert.Contains(DateTime.Now.ToString("yyyy-MM-dd"), snippet.Title);
            Assert.False(snippet.IsGenerated); // Default for this constructor
            Assert.True(snippet.CreatedAt >= beforeCreation && snippet.CreatedAt <= afterCreation.AddMilliseconds(100)); // Allow some leeway
        }

        [Theory]
        [InlineData(0, "easy")]
        [InlineData(1, "medium")]
        [InlineData(2, "hard")]
        [InlineData(3, "medium")] // Default for out of range
        [InlineData(-1, "medium")] // Default for out of range
        public void Constructor_WithContentIntDifficultyLanguage_ShouldMapDifficultyCorrectly(int intDifficulty, string expectedStringDifficulty)
        {
            // Arrange
            var content = "Another test snippet.";
            var language = "de";
            var beforeCreation = DateTime.UtcNow;

            // Act
            var snippet = new TextSnippet(content, intDifficulty, language);
            var afterCreation = DateTime.UtcNow;

            // Assert
            Assert.Equal(content, snippet.Content);
            Assert.Equal(expectedStringDifficulty, snippet.Difficulty);
            Assert.Equal(language, snippet.Language);
            Assert.StartsWith("Text ", snippet.Title);
            Assert.Contains(DateTime.Now.ToString("yyyy-MM-dd"), snippet.Title);
            Assert.False(snippet.IsGenerated);
            Assert.True(snippet.CreatedAt >= beforeCreation && snippet.CreatedAt <= afterCreation.AddMilliseconds(100));
        }

        [Fact]
        public void Constructor_WithContentDifficulty_ShouldUseDefaultLanguageRu()
        {
            // Arrange
            var content = "Тестовый фрагмент текста.";
            var difficulty = "easy";

            // Act
            var snippet = new TextSnippet(content, difficulty);

            // Assert
            Assert.Equal("ru", snippet.Language);
        }

        [Fact]
        public void Constructor_WithContentIntDifficulty_ShouldUseDefaultLanguageRu()
        {
            // Arrange
            var content = "Тестовый фрагмент текста с числовой сложностью.";
            var intDifficulty = 1; // medium

            // Act
            var snippet = new TextSnippet(content, intDifficulty);

            // Assert
            Assert.Equal("ru", snippet.Language);
            Assert.Equal("medium", snippet.Difficulty);
        }
    }
}
