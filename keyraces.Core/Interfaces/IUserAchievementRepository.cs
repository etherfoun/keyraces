using keyraces.Core.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace keyraces.Core.Interfaces
{
    public interface IUserAchievementRepository
    {
        Task<UserAchievement?> GetByIdAsync(int id);
        Task<IEnumerable<UserAchievement>> ListAllAsync();
        Task AddAsync(UserAchievement userAchievement);
        Task UpdateAsync(UserAchievement userAchievement);
        Task DeleteAsync(int id);
        Task<UserAchievement?> FindByUserAndAchievementAsync(int userProfileId, int achievementId);
        Task<IEnumerable<UserAchievement>> ListByUserWithAchievementDataAsync(int userProfileId);
    }
}
