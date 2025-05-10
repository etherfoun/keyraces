using keyraces.Core.Entities;
using keyraces.Core.Interfaces;
using keyraces.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace keyraces.Infrastructure.Repositories
{
    public class AchievementRepository : IAchievementRepository
    {
        private readonly AppDbContext _ctx;
        public AchievementRepository(AppDbContext ctx) => _ctx = ctx;

        public async Task<Achievement> GetByIdAsync(int id)
        {
            var achievement = await _ctx.Achievements.FindAsync(id);
            if (achievement == null)
            {
                throw new InvalidOperationException($"Achievement with ID {id} not found.");
            }
            return achievement;
        }

        public async Task<IEnumerable<Achievement>> ListAsync() => await _ctx.Achievements.ToListAsync();

        public async Task AddAsync(Achievement a)
        {
            _ctx.Achievements.Add(a);
            await _ctx.SaveChangesAsync();
        }

        public async Task UpdateAsync(Achievement a)
        {
            _ctx.Achievements.Update(a);
            await _ctx.SaveChangesAsync();
        }
    }
}
