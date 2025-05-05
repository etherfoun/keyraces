using keyraces.Core.Entities;

namespace keyraces.Core.Interfaces
{
    public interface ILeaderboardService
    {
        Task<IEnumerable<LeaderboardEntry>> GetLeaderboardAsync(int competitionId);
    }
}
