using keyraces.Core.Interfaces;

namespace keyraces.Infrastructure.Services
{
    public class TypingStatisticService : ITypingStatisticService
    {
        private readonly ITypingSessionRepository _sessionRepo;
        private readonly ITypingStatisticRepository _statRepo;

        public TypingStatisticService(
            ITypingStatisticRepository statRepo,
            ITypingSessionRepository sessionRepo)
        {
            _statRepo = statRepo;
            _sessionRepo = sessionRepo;
        }
        public async Task<TypingStatistic> CreateAsync(int sessionId, double wpm, int errors)
        {
            var session = await _sessionRepo.GetByIdAsync(sessionId);
            if (session is null)
                throw new InvalidOperationException($"Session {sessionId} not found");

            var elapsedSeconds = (session.EndTime - session.StartTime).TotalSeconds;
            var totalChars = elapsedSeconds * wpm;
            var accuracy = totalChars > 0
                ? Math.Max(0.0, 1.0 - errors / totalChars)
                : 0.0;

            var stat = new TypingStatistic(
                session.UserId,
                sessionId,
                wpm,
                accuracy
            );

            await _statRepo.AddAsync(stat);

            return stat;
        }
        public async Task UpdateAsync(int sessionId, double newWpm, double newAccuracy)
        {
            var stat = await _statRepo.GetBySessionIdAsync(sessionId);
            if (stat is null)
                throw new InvalidOperationException($"Statistic for session {sessionId} not found");

            stat.Update(newWpm, newAccuracy);
            await _statRepo.UpdateAsync(stat);
        }
        public Task<TypingStatistic> GetBySessionAsync(int sessionId) =>
            _statRepo.GetBySessionIdAsync(sessionId);
    }
}
