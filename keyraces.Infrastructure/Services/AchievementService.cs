using keyraces.Core.Entities;
using keyraces.Core.Enums;
using keyraces.Core.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace keyraces.Infrastructure.Services
{
    public class AchievementService : IAchievementService
    {
        private readonly IAchievementRepository _achievementRepository;

        public AchievementService(IAchievementRepository achievementRepository)
        {
            _achievementRepository = achievementRepository;
        }

        public async Task<Achievement> CreateAchievementAsync(string name, string description, AchievementKey key, string? iconCssClass = null)
        {
            var existingAchievement = await _achievementRepository.GetByKeyAsync(key);
            if (existingAchievement != null)
            {
                throw new System.InvalidOperationException($"Achievement with key {key} already exists.");
            }
            var achievement = new Achievement(key, name, description) { IconCssClass = iconCssClass };
            await _achievementRepository.AddAsync(achievement);
            return achievement;
        }

        public async Task<Achievement?> GetByIdAsync(int id)
        {
            return await _achievementRepository.GetByIdAsync(id);
        }

        public async Task<Achievement?> GetByKeyAsync(AchievementKey key)
        {
            return await _achievementRepository.GetByKeyAsync(key);
        }

        public async Task<IEnumerable<Achievement>> ListAsync()
        {
            return await _achievementRepository.ListAllAsync();
        }

        public async Task UpdateAsync(Achievement achievement)
        {
            await _achievementRepository.UpdateAsync(achievement);
        }

        public async Task DeleteAsync(int id)
        {
            await _achievementRepository.DeleteAsync(id);
        }
    }
}
