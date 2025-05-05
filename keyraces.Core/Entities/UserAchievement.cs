namespace keyraces.Core.Entities
{
    public class UserAchievement
    {
        protected UserAchievement() { }

        public UserAchievement(int userId, int achievementId)
        {
            UserId = userId;
            AchievementId = achievementId;
            AwardedAt = DateTime.UtcNow;
        }

        public int UserId { get; private set; }
        public UserProfile User { get; private set; } = null!;

        public int AchievementId { get; private set; }
        public Achievement Achievement { get; private set; } = null!;

        public DateTime AwardedAt { get; private set; }
    }
}
