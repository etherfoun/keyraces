using keyraces.Core.Entities;
using keyraces.Core.Enums;
using keyraces.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace keyraces.Infrastructure.Services
{
    public class AchievementCheckerService : IAchievementCheckerService
    {
        private readonly IUserProfileService _userProfileService;
        private readonly IUserAchievementService _userAchievementService;
        private readonly IAchievementService _achievementService;
        private readonly ITypingSessionRepository _typingSessionRepository;
        private readonly ILogger<AchievementCheckerService> _logger;

        public AchievementCheckerService(
            IUserProfileService userProfileService,
            IUserAchievementService userAchievementService,
            IAchievementService achievementService,
            ITypingSessionRepository typingSessionRepository,
            ILogger<AchievementCheckerService> logger)
        {
            _userProfileService = userProfileService;
            _userAchievementService = userAchievementService;
            _achievementService = achievementService;
            _typingSessionRepository = typingSessionRepository;
            _logger = logger;
        }

        public async Task CheckAndAwardAchievementsAfterSessionAsync(string identityUserId, TypingSessionResult sessionResult)
        {
            UserProfile userProfile;
            try
            {
                userProfile = await _userProfileService.GetByIdentityIdAsync(identityUserId);
                if (userProfile == null)
                {
                    _logger.LogWarning("User profile not found for identityUserId {IdentityUserId} while checking achievements.", identityUserId);
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching user profile for identityUserId {IdentityUserId} while checking achievements.", identityUserId);
                return;
            }

            var userProfileId = userProfile.Id;
            var awardedAchievementIds = (await _userAchievementService.GetUserAchievementsAsync(userProfileId))
                                        .Select(ua => ua.AchievementId)
                                        .ToHashSet();

            await TryAwardSingleAchievementAsync(userProfileId, AchievementKey.FirstSessionCompleted, awardedAchievementIds,
                async () => true);

            await TryAwardSingleAchievementAsync(userProfileId, AchievementKey.SpeedDemon50WPM, awardedAchievementIds,
                async () => sessionResult.WPM >= 50);

            await TryAwardSingleAchievementAsync(userProfileId, AchievementKey.SpeedDemon75WPM, awardedAchievementIds,
                async () => sessionResult.WPM >= 75);

            await TryAwardSingleAchievementAsync(userProfileId, AchievementKey.SpeedDemon100WPM, awardedAchievementIds,
                async () => sessionResult.WPM >= 100);

            await TryAwardSingleAchievementAsync(userProfileId, AchievementKey.AccuracyMaster98, awardedAchievementIds,
                async () => sessionResult.Accuracy >= 0.98);

            await TryAwardSingleAchievementAsync(userProfileId, AchievementKey.AccuracyMaster99, awardedAchievementIds,
                async () => sessionResult.Accuracy >= 0.99);

            await TryAwardSingleAchievementAsync(userProfileId, AchievementKey.AccuracyMaster100, awardedAchievementIds,
                async () => sessionResult.Accuracy >= 1.00);

            var sessionCount = await _typingSessionRepository.CountSessionsByUserProfileIdAsync(userProfileId);

            await TryAwardSingleAchievementAsync(userProfileId, AchievementKey.TenSessionsCompleted, awardedAchievementIds,
                async () => sessionCount >= 10);

            await TryAwardSingleAchievementAsync(userProfileId, AchievementKey.FiftySessionsCompleted, awardedAchievementIds,
                async () => sessionCount >= 50);
        }

        private async Task TryAwardSingleAchievementAsync(int userProfileId, AchievementKey achievementKey, ISet<int> awardedAchievementIds, Func<Task<bool>> conditionChecker)
        {
            var achievementDefinition = await _achievementService.GetByKeyAsync(achievementKey);
            if (achievementDefinition == null)
            {
                _logger.LogWarning("Achievement definition not found for key {AchievementKey}.", achievementKey);
                return;
            }

            if (awardedAchievementIds.Contains(achievementDefinition.Id))
            {
                return;
            }

            if (await conditionChecker())
            {
                try
                {
                    await _userAchievementService.AwardAsync(userProfileId, achievementDefinition.Id);
                    _logger.LogInformation("Awarded achievement {AchievementName} (Key: {AchievementKey}) to user profile {UserProfileId}",
                        achievementDefinition.Name, achievementKey, userProfileId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to award achievement {AchievementKey} to user profile {UserProfileId}.", achievementKey, userProfileId);
                }
            }
        }
    }
}
