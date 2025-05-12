namespace keyraces.Core.Entities
{
    public class TypingSession
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int TextSnippetId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public DateTime CompletedAt => EndTime;

        public UserProfile User { get; set; } = null!;
        public TextSnippet TextSnippet { get; set; } = null!;

        protected TypingSession() { }

        public TypingSession(int userId, int textSnippetId)
        {
            UserId = userId;
            TextSnippetId = textSnippetId;
            StartTime = DateTime.UtcNow;
        }

        public void Complete()
        {
            EndTime = DateTime.UtcNow;
        }
    }
}
