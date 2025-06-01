using keyraces.Core.Enums;
using System.Collections.Generic;

namespace keyraces.Core.Entities
{
    public class Achievement
    {
        protected Achievement() { Key = AchievementKey.Unknown; Name = string.Empty; Description = string.Empty; }

        public Achievement(AchievementKey key, string name, string description)
        {
            Key = key;
            Name = name;
            Description = description;
        }

        public int Id { get; private set; }
        public AchievementKey Key { get; private set; }
        public string Name { get; private set; }
        public string Description { get; private set; }
        public string? IconCssClass { get; set; }

        public ICollection<UserAchievement> AwardedTo { get; private set; } = new List<UserAchievement>();
    }
}
