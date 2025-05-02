using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace keyraces.Core.Entities
{
    public enum ParticipantStatus { NotStarted, Typing, Completed }

    public class CompetitionParticipant
    {
        protected CompetitionParticipant() { }

        public CompetitionParticipant(int competitionId, int userId)
        {
            CompetitionId = competitionId;
            UserId = userId;
            JoinedAt = DateTime.UtcNow;
            Status = ParticipantStatus.NotStarted;
        }

        public int CompetitionId { get; private set; }
        public Competition Competition { get; private set; }

        public int UserId { get; private set; }
        public UserProfile UserProfile { get; private set; }

        public DateTime JoinedAt { get; private set; }
        public DateTime? FinishedAt { get; private set; }

        public double? WPM { get; private set; }
        public int? ErrorCount { get; private set; }
        public ParticipantStatus Status { get; private set; }

        public void Begin()
        {
            Status = ParticipantStatus.Typing;
        }

        public void Complete(double wpm, int errors)
        {
            WPM = wpm;
            ErrorCount = errors;
            FinishedAt = DateTime.UtcNow;
            Status = ParticipantStatus.Completed;
        }
    }
}
