using keyraces.Core.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace keyraces.Core.Interfaces
{
    public interface IUserAchievementService
    {
        Task AwardAsync(int userProfileId, int achievementId);
        Task<IEnumerable<UserAchievement>> GetUserAchievementsAsync(int userProfileId);
    }
}
