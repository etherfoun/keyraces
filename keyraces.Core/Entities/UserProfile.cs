namespace keyraces.Core.Entities
{
    public class UserProfile
    {
        public int Id { get; set; }
        public string IdentityUserId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public ICollection<UserAchievement> Achievements { get; set; }
            = new List<UserAchievement>();

        protected UserProfile() { }
        public UserProfile(string identityUserId, string name)
        {
            IdentityUserId = identityUserId;
            Name = name;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }
        public void Update(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name cannot be empty.", nameof(name));

            Name = name;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
