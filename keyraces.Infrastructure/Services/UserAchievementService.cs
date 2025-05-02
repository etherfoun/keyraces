using keyraces.Core.Entities;
using keyraces.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace keyraces.Infrastructure.Services
{
    public class UserAchievementService : IUserAchievementService
    {
        private readonly IUserAchievementRepository _uaRepo;

        public UserAchievementService(IUserAchievementRepository uaRepo)
        {
            _uaRepo = uaRepo;
        }

        public Task<IEnumerable<UserAchievement>> GetUserAchievementsAsync(int userId)
        {
            return _uaRepo.ListByUserAsync(userId);
        }

        public Task AwardAsync(int userId, int achievementId)
        {
            var ua = new UserAchievement(userId, achievementId);
            return _uaRepo.AddAsync(ua);
        }
    }
}
