namespace keyraces.Core.Entities
{
    public enum CompetitionStatus { Scheduled, InProgress, Finished}

    public class Competition
    {
        protected Competition() { }

        public Competition(string title, int textSnippetId, DateTime startTime)
        {
            Title = title;
            TextSnippetId = textSnippetId;
            StartTime = startTime;
            Status = CompetitionStatus.Scheduled;
        }

        public int Id { get; private set; }
        public string Title { get; private set; } = string.Empty;
        public int TextSnippetId { get; private set; }
        public DateTime StartTime { get; private set; }
        public DateTime? EndTime { get; private set; }
        public CompetitionStatus Status { get; private set; }

        public ICollection<CompetitionParticipant> Participants { get; private set; } = new List<CompetitionParticipant>();

        public void Start()
        {
            Status = CompetitionStatus.InProgress;
        }

        public void Finish()
        {
            Status = CompetitionStatus.Finished;
            EndTime = DateTime.UtcNow;
        }
    }
}
