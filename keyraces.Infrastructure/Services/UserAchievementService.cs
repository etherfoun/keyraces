using keyraces.Core.Entities;
using keyraces.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace keyraces.Infrastructure.Services
{
    public class UserAchievementService : IUserAchievementService
    {
        private readonly IUserAchievementRepository _userAchievementRepository;
        private readonly ILogger<UserAchievementService> _logger;

        public UserAchievementService(IUserAchievementRepository userAchievementRepository, ILogger<UserAchievementService> logger)
        {
            _userAchievementRepository = userAchievementRepository;
            _logger = logger;
        }

        public async Task AwardAsync(int userProfileId, int achievementId)
        {
            var existing = await _userAchievementRepository.FindByUserAndAchievementAsync(userProfileId, achievementId);
            if (existing != null)
            {
                _logger.LogInformation("Achievement {AchievementId} already awarded to user profile {UserProfileId}. Skipping.", achievementId, userProfileId);
                return;
            }

            var userAchievement = new UserAchievement(userProfileId, achievementId);
            await _userAchievementRepository.AddAsync(userAchievement);
            _logger.LogInformation("Successfully awarded achievement {AchievementId} to user profile {UserProfileId}.", achievementId, userProfileId);
        }

        public async Task<IEnumerable<UserAchievement>> GetUserAchievementsAsync(int userProfileId)
        {
            return await _userAchievementRepository.ListByUserWithAchievementDataAsync(userProfileId);
        }
    }
}
