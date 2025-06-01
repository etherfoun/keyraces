using keyraces.Core.Entities;

namespace keyraces.Core.Interfaces
{
    public interface ITypingSessionRepository
    {
        Task<TypingSession> AddAsync(TypingSession entity);
        Task<TypingSession?> GetByIdAsync(int id);
        Task<IEnumerable<TypingSession>> ListByUserAsync(int userId);
        Task UpdateAsync(TypingSession entity);
        Task<int> CountSessionsByUserProfileIdAsync(int userProfileId);
    }
}
