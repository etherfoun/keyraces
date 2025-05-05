using keyraces.Core.Entities;

namespace keyraces.Core.Interfaces
{
    public interface IUserAchievementService
    {
        Task<IEnumerable<UserAchievement>> GetUserAchievementsAsync(int userId);
        Task AwardAsync(int userId, int achievementId);
    }
}
