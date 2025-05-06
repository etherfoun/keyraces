using keyraces.Core.Entities;
using keyraces.Core.Interfaces;

namespace keyraces.Infrastructure.Services
{
    public class TypingSessionService : ITypingSessionService
    {
        private readonly ISessionRepository _repo;

        public TypingSessionService(ISessionRepository repo)
        {
            _repo = repo;
        }

        public async Task<TypingSession> StartSessionAsync(int userId, int textSnippetId)
        {
            var session = new TypingSession(userId, textSnippetId);
            await _repo.AddAsync(session);
            return session;
        }

        public async Task CompleteSessionAsync(int sessionId, DateTime endTime)
        {
            var session = await _repo.GetByIdAsync(sessionId);
            session.EndTime = endTime;
            await _repo.UpdateAsync(session);
        }

        public Task<IEnumerable<TypingSession>> GetByUserAsync(int userId) =>
            _repo.ListByUserAsync(userId);

        public Task<TypingSession?> GetByIdAsync(int sessionId) => _repo.GetByIdAsync(sessionId);
    }
}
