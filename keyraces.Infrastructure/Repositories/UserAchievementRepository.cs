using keyraces.Core.Entities;
using keyraces.Core.Interfaces;
using keyraces.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace keyraces.Infrastructure.Repositories
{
    public class UserAchievementRepository : IUserAchievementRepository
    {
        private readonly AppDbContext _ctx;
        public UserAchievementRepository(AppDbContext ctx) => _ctx = ctx;
        public async Task<IEnumerable<UserAchievement>> ListByUserAsync(int userId) =>
            await _ctx.UserAchievements.Where(ua => ua.UserId == userId).ToListAsync();
        public async Task AddAsync(UserAchievement ua) { _ctx.UserAchievements.Add(ua); await _ctx.SaveChangesAsync(); }
    }
}
