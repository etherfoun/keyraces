namespace keyraces.Core.Entities
{
    public class TypingStatistic
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int SessionId { get; set; }
        public double WPM { get; set; }
        public double Accuracy { get; set; }

        protected TypingStatistic() { } // For EF Core

        public TypingStatistic(int userId, int sessionId, double wpm, double accuracy)
        {
            UserId = userId;
            SessionId = sessionId;
            WPM = wpm;
            Accuracy = accuracy;
        }
        public void Update(double wpm, double accuracy)
        {
            WPM = wpm;
            Accuracy = accuracy;
        }
    }
}
