using keyraces.Core.Entities;

namespace keyraces.Core.Interfaces
{
    public interface ITypingStatisticService
    {
        Task<TypingStatistic> CreateAsync(int userId, int sessionId, double wpm, double accuracy);
        Task UpdateAsync(int id, double newWpm, double newAccuracy);
        Task<TypingStatistic> GetBySessionAsync(int sessionId);
    }
}
