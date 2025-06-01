using keyraces.Core.Entities;
using keyraces.Core.Enums;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace keyraces.Core.Interfaces
{
    public interface IAchievementService
    {
        Task<Achievement> CreateAchievementAsync(string name, string description, AchievementKey key, string? iconCssClass = null);
        Task<Achievement?> GetByIdAsync(int id);
        Task<Achievement?> GetByKeyAsync(AchievementKey key);
        Task<IEnumerable<Achievement>> ListAsync();
        Task UpdateAsync(Achievement achievement);
        Task DeleteAsync(int id);
    }
}
