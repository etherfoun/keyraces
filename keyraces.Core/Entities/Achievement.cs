namespace keyraces.Core.Entities
{
    public class Achievement
    {
        protected Achievement() { }

        public Achievement(string name, string description)
        {
            Name = name;
            Description = description;
        }

        public int Id { get; private set; }
        public string Name { get; private set; } = string.Empty;
        public string Description { get; private set; } = string.Empty;

        public ICollection<UserAchievement> AwardedTo { get; private set; } = new List<UserAchievement>();
    }
}
