using keyraces.Core.Entities;
using keyraces.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace keyraces.Infrastructure.Services
{
    public class AchievementService : IAchievementService
    {
        private readonly IAchievementRepository _repo;
        private readonly IUserAchievementRepository _uaRepo;
        public AchievementService(
            IAchievementRepository repo,
            IUserAchievementRepository uaRepo)
        {
            _repo = repo;
            _uaRepo = uaRepo;
        }
        public Task<IEnumerable<Achievement>> ListAsync()
    => _repo.ListAsync();
        public async Task<Achievement> CreateAchievementAsync(string name, string description)
        {
            var a = new Achievement(name, description);
            await _repo.AddAsync(a);
            return a;
        }
        public async Task AwardAsync(int userId, int achievementId)
        {
            var ua = new UserAchievement(userId, achievementId);
            await _uaRepo.AddAsync(ua);
        }
        public Task<IEnumerable<UserAchievement>> GetUserAchievementsAsync(int userId) =>
            _uaRepo.ListByUserAsync(userId);
    }
}
