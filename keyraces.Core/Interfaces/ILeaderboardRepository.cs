using keyraces.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace keyraces.Core.Interfaces
{
    public interface ILeaderboardRepository
    {
        Task<IEnumerable<LeaderboardEntry>> ListByCompetitionAsync(int competitionId);
        Task AddAsync(LeaderboardEntry entry);
        Task UpdateAsync(LeaderboardEntry entry);
    }
}
