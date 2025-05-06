using keyraces.Core.Entities;

namespace keyraces.Core.Interfaces
{
    public interface ITypingSessionService
    {
        Task<TypingSession> StartSessionAsync(int userId, int textSnippetId);
        Task CompleteSessionAsync(int sessionId, DateTime endTime);
        Task<IEnumerable<TypingSession>> GetByUserAsync(int userId);
        Task<TypingSession?> GetByIdAsync(int sessionId);
    }
}
