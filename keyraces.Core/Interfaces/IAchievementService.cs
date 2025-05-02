using keyraces.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace keyraces.Core.Interfaces
{
    public interface IAchievementService
    {
        Task<IEnumerable<Achievement>> ListAsync();
        Task<Achievement> CreateAchievementAsync(string name, string description);
        Task AwardAsync(int userId, int achievementId);
        Task<IEnumerable<UserAchievement>> GetUserAchievementsAsync(int userId);
    }
}
