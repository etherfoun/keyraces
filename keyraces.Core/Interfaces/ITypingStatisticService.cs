namespace keyraces.Core.Interfaces
{
    public interface ITypingStatisticService
    {
        Task<TypingStatistic> CreateAsync(int sessionId, double wpm, int errors);
        Task UpdateAsync(int sessionId, double newWpm, double newAccuracy);
        Task<TypingStatistic> GetBySessionAsync(int sessionId);
    }
}
