using keyraces.Core.Entities;
using keyraces.Core.Interfaces;
using keyraces.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace keyraces.Infrastructure.Repositories
{
    public class UserAchievementRepository : IUserAchievementRepository
    {
        private readonly AppDbContext _context;

        public UserAchievementRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<UserAchievement?> GetByIdAsync(int id)
        {
            return await _context.UserAchievements
                .Include(ua => ua.Achievement)
                .Include(ua => ua.UserProfile)
                .FirstOrDefaultAsync(ua => ua.Id == id);
        }

        public async Task<IEnumerable<UserAchievement>> ListAllAsync()
        {
            return await _context.UserAchievements
               .Include(ua => ua.Achievement)
               .Include(ua => ua.UserProfile)
               .ToListAsync();
        }

        public async Task AddAsync(UserAchievement userAchievement)
        {
            _context.UserAchievements.Add(userAchievement);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(UserAchievement userAchievement)
        {
            _context.Entry(userAchievement).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var userAchievement = await _context.UserAchievements.FindAsync(id);
            if (userAchievement != null)
            {
                _context.UserAchievements.Remove(userAchievement);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<UserAchievement?> FindByUserAndAchievementAsync(int userProfileId, int achievementId)
        {
            return await _context.UserAchievements
                .FirstOrDefaultAsync(ua => ua.UserProfileId == userProfileId && ua.AchievementId == achievementId);
        }

        public async Task<IEnumerable<UserAchievement>> ListByUserWithAchievementDataAsync(int userProfileId)
        {
            return await _context.UserAchievements
                .Include(ua => ua.Achievement)
                .Where(ua => ua.UserProfileId == userProfileId)
                .OrderByDescending(ua => ua.AwardedAt)
                .ToListAsync();
        }
    }
}
