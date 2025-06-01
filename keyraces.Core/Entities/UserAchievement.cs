using System;

namespace keyraces.Core.Entities
{
    public class UserAchievement
    {
        protected UserAchievement() { }

        public UserAchievement(int userProfileId, int achievementId)
        {
            UserProfileId = userProfileId;
            AchievementId = achievementId;
            AwardedAt = DateTime.UtcNow;
        }

        public int Id { get; private set; }
        public int UserProfileId { get; private set; }
        public UserProfile? UserProfile { get; private set; }
        public int AchievementId { get; private set; }
        public Achievement? Achievement { get; private set; }
        public DateTime AwardedAt { get; private set; }
    }
}
