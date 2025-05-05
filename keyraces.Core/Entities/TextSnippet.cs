namespace keyraces.Core.Entities
{
    public class TextSnippet
    {
        public int Id { get; private set; }

        public string Content { get; private set; } = string.Empty;

        public DifficultyLevel Difficulty { get; private set; }

        protected TextSnippet() { }

        public TextSnippet(string content, DifficultyLevel difficulty)
        {
            Content = content ?? throw new ArgumentNullException(nameof(content));
            Difficulty = difficulty;
        }

        public void Update(string content, DifficultyLevel difficulty)
        {
            Content = content ?? throw new ArgumentNullException(nameof(content));
            Difficulty = difficulty;
        }
    }
}