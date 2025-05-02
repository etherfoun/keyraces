using keyraces.Core.Entities;
using keyraces.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace keyraces.Infrastructure.Services
{
    public class TypingStatisticService : ITypingStatisticService
    {
        private readonly ITypingStatisticRepository _repo;

        public TypingStatisticService(ITypingStatisticRepository repo)
        {
            _repo = repo;
        }

        public async Task<TypingStatistic> CreateAsync(int userId, int sessionId, double wpm, double accuracy)
        {
            var stat = new TypingStatistic(userId, sessionId, wpm, accuracy);
            await _repo.AddAsync(stat);
            return stat;
        }

        public async Task UpdateAsync(int id, double newWpm, double newAccuracy)
        {
            var stat = await _repo.GetBySessionIdAsync(id);
            stat.Update(newWpm, newAccuracy);
            await _repo.UpdateAsync(stat);
        }

        public Task<TypingStatistic> GetBySessionAsync(int sessionId) =>
            _repo.GetBySessionIdAsync(sessionId);
    }
}
