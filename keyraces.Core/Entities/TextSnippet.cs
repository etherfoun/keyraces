namespace keyraces.Core.Entities
{
    public class TextSnippet
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string Difficulty { get; set; } = "medium";
        public bool IsGenerated { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string Language { get; set; } = "ru";

        public TextSnippet()
        {
        }

        public TextSnippet(string content, string difficulty, string language = "ru")
        {
            Content = content;
            Difficulty = difficulty;
            Language = language;
            Title = $"Text {DateTime.Now:yyyy-MM-dd}";
            CreatedAt = DateTime.UtcNow;
        }

        public TextSnippet(string content, int difficulty, string language = "ru")
        {
            Content = content;
            Difficulty = difficulty switch
            {
                0 => "easy",
                1 => "medium",
                2 => "hard",
                _ => "medium"
            };
            Language = language;
            Title = $"Text {DateTime.Now:yyyy-MM-dd}";
            CreatedAt = DateTime.UtcNow;
        }
    }
}
