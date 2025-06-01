using keyraces.Core.Entities;
using keyraces.Core.Enums;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace keyraces.Core.Interfaces
{
    public interface IAchievementRepository
    {
        Task<Achievement?> GetByIdAsync(int id);
        Task<Achievement?> GetByKeyAsync(AchievementKey key);
        Task<IEnumerable<Achievement>> ListAllAsync();
        Task AddAsync(Achievement achievement);
        Task UpdateAsync(Achievement achievement);
        Task DeleteAsync(int id);
    }
}
