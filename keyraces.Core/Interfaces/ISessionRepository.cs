using keyraces.Core.Entities;

namespace keyraces.Core.Interfaces
{
    public interface ISessionRepository
    {
        Task<TypingSession> GetByIdAsync(int id);
        Task<IEnumerable<TypingSession>> ListByUserAsync(int userId);
        Task AddAsync(TypingSession session);
        Task UpdateAsync(TypingSession session);
        int? GetCurrentUserId();
        string? GetCurrentUsername();
        Task ClearSessionAsync();
        Task ClearCurrentUserAsync();
        Task SetCurrentUserAsync(int userId, string username);
    }
}
