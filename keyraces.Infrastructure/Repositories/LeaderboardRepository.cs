using keyraces.Core.Entities;
using keyraces.Core.Interfaces;
using keyraces.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace keyraces.Infrastructure.Repositories
{
    public class LeaderboardRepository : ILeaderboardRepository
    {
        private readonly AppDbContext _ctx;
        public LeaderboardRepository(AppDbContext ctx) => _ctx = ctx;
        public async Task<IEnumerable<LeaderboardEntry>> ListByCompetitionAsync(int compId) =>
            await _ctx.LeaderboardEntries.Where(e => e.CompetitionId == compId)
                                         .OrderBy(e => e.Rank)
                                         .ToListAsync();
        public async Task AddAsync(LeaderboardEntry e) { _ctx.LeaderboardEntries.Add(e); await _ctx.SaveChangesAsync(); }
        public async Task UpdateAsync(LeaderboardEntry e) { _ctx.LeaderboardEntries.Update(e); await _ctx.SaveChangesAsync(); }
    }
}
