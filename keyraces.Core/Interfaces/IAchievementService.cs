using keyraces.Core.Entities;

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
