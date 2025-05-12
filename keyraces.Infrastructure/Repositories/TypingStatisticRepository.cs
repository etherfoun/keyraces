using keyraces.Core.Interfaces;
using keyraces.Infrastructure.Data;

namespace keyraces.Infrastructure.Repositories
{
    public class TypingStatisticRepository : ITypingStatisticRepository
    {
        private readonly AppDbContext _ctx;
        public TypingStatisticRepository(AppDbContext ctx) => _ctx = ctx;

        public async Task<TypingStatistic> GetBySessionIdAsync(int sessionId)
        {
            var session = await _ctx.Statistics.FindAsync(sessionId);
            if (session == null)
            {
                throw new InvalidOperationException($"TextSnippet with ID {sessionId} not found.");
            }
            return session;
        }

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
