using keyraces.Core.Entities;

namespace keyraces.Core.Interfaces
{
    public interface IAchievementRepository
    {
        Task<Achievement> GetByIdAsync(int id);
        Task<IEnumerable<Achievement>> ListAsync();
        Task AddAsync(Achievement achievement);
        Task UpdateAsync(Achievement achievement);
    }
}
