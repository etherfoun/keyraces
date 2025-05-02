using keyraces.Core.Entities;
using keyraces.Core.Interfaces;
using keyraces.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace keyraces.Infrastructure.Repositories
{
    public class TypingStatisticRepository : ITypingStatisticRepository
    {
        private readonly AppDbContext _ctx;
        public TypingStatisticRepository(AppDbContext ctx) => _ctx = ctx;

        public async Task<TypingStatistic> GetBySessionIdAsync(int sessionId) =>
            await _ctx.Statistics
                      .FirstOrDefaultAsync(st => st.SessionId == sessionId);

        public async Task AddAsync(TypingStatistic stat)
        {
            _ctx.Statistics.Add(stat);
            await _ctx.SaveChangesAsync();
        }

        public async Task UpdateAsync(TypingStatistic stat)
        {
            _ctx.Statistics.Update(stat);
            await _ctx.SaveChangesAsync();
        }
    }
}
