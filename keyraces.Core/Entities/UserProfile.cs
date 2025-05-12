namespace keyraces.Core.Entities
{
    public class UserProfile
    {
        public int Id { get; set; }
        public string IdentityUserId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime JoinedDate { get; set; }
        public double AverageWPM { get; set; }
        public double AverageAccuracy { get; set; }
        public int TotalRaces { get; set; }
        public int TotalPractices { get; set; }
        public ICollection<UserAchievement> Achievements { get; set; }
            = new List<UserAchievement>();

        protected UserProfile() { }

        public UserProfile(string identityUserId, string name)
        {
            IdentityUserId = identityUserId;
            Name = name;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
            JoinedDate = DateTime.UtcNow;
            AverageWPM = 0;
            AverageAccuracy = 0;
            TotalRaces = 0;
            TotalPractices = 0;
        }

        public void Update(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name cannot be empty.", nameof(name));

            Name = name;
            UpdatedAt = DateTime.UtcNow;
        }

        public void UpdateStatistics(double wpm, double accuracy, bool isRace)
        {
            double totalRacesAndPractices = TotalRaces + TotalPractices;

            if (totalRacesAndPractices > 0)
            {
                AverageWPM = ((AverageWPM * totalRacesAndPractices) + wpm) / (totalRacesAndPractices + 1);
                AverageAccuracy = ((AverageAccuracy * totalRacesAndPractices) + accuracy) / (totalRacesAndPractices + 1);
            }
            else
            {
                AverageWPM = wpm;
                AverageAccuracy = accuracy;
            }

            if (isRace)
                TotalRaces++;
            else
                TotalPractices++;

            UpdatedAt = DateTime.UtcNow;
        }
    }
}
