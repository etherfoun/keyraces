using keyraces.Core.Enums;
using System;

namespace keyraces.Server.Dtos
{
    public class UserAchievementDisplayDto
    {
        public int AchievementId { get; set; }
        public AchievementKey Key { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime AwardedAt { get; set; }
        public string? IconCssClass { get; set; }
    }
}
