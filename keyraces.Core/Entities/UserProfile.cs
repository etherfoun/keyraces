namespace keyraces.Core.Entities
{
    public class UserProfile
    {
        public int Id { get; set; }
        public string IdentityUserId { get; set; }
        public string Name { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public ICollection<UserAchievement> Achievements { get; set; }
            = new List<UserAchievement>();

        protected UserProfile() { }
        public UserProfile(string identityUserId, string name)
        {
            IdentityUserId = identityUserId;
            Name = name;
            CreatedAt = UpdatedAt = DateTime.UtcNow;
        }
        public void Update(string name)
        {
            Name = name;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
