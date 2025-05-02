using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace keyraces.Core.Entities
{
    public class LeaderboardEntry
    {
        protected LeaderboardEntry() { }

        public LeaderboardEntry(int competitionId, int userId, int rank, double score)
        {
            CompetitionId = competitionId;
            UserId = userId;
            Rank = rank;
            Score = score;
        }

        public int Id { get; private set; }
        public int CompetitionId { get; private set; }
        public Competition Competition { get; private set; }

        public int UserId { get; private set; }
        public UserProfile User { get; private set; }

        public int Rank { get; private set; }
        public double Score { get; private set; }
    }
}
