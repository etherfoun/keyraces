using keyraces.Core.Entities;

namespace keyraces.Core.Interfaces
{
    public interface ILeaderboardRepository
    {
        Task<IEnumerable<LeaderboardEntry>> ListByCompetitionAsync(int competitionId);
        Task AddAsync(LeaderboardEntry entry);
        Task UpdateAsync(LeaderboardEntry entry);
    }
}
