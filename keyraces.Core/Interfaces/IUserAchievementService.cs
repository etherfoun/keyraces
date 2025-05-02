using keyraces.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace keyraces.Core.Interfaces
{
    public interface IUserAchievementService
    {
        Task<IEnumerable<UserAchievement>> GetUserAchievementsAsync(int userId);
        Task AwardAsync(int userId, int achievementId);
    }
}
