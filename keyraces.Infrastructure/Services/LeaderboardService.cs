using keyraces.Core.Entities;
using keyraces.Core.Interfaces;

namespace keyraces.Infrastructure.Services
{
    public class LeaderboardService : ILeaderboardService
    {
        private readonly ILeaderboardRepository _repo;
        public LeaderboardService(ILeaderboardRepository repo) => _repo = repo;
        public Task<IEnumerable<LeaderboardEntry>> GetLeaderboardAsync(int compId) =>
            _repo.ListByCompetitionAsync(compId);
    }
}
