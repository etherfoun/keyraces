using keyraces.Core.Entities;

namespace keyraces.Core.Interfaces
{
    public interface IUserAchievementRepository
    {
        Task<IEnumerable<UserAchievement>> ListByUserAsync(int userId);
        Task AddAsync(UserAchievement ua);
    }
}
