using keyraces.Core.Entities;

namespace keyraces.Core.Interfaces
{
    public interface ITypingStatisticRepository
    {
        Task<TypingStatistic> GetBySessionIdAsync(int sessionId);
        Task AddAsync(TypingStatistic statistic);
        Task UpdateAsync(TypingStatistic statistic);
    }
}
