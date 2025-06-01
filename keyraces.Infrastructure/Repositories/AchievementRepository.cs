using keyraces.Core.Entities;
using keyraces.Core.Enums;
using keyraces.Core.Interfaces;
using keyraces.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace keyraces.Infrastructure.Repositories
{
    public class AchievementRepository : IAchievementRepository
    {
        private readonly AppDbContext _context;

        public AchievementRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Achievement?> GetByIdAsync(int id)
        {
            return await _context.Achievements.FindAsync(id);
        }

        public async Task<Achievement?> GetByKeyAsync(AchievementKey key)
        {
            return await _context.Achievements.FirstOrDefaultAsync(a => a.Key == key);
        }

        public async Task<IEnumerable<Achievement>> ListAllAsync()
        {
            return await _context.Achievements.ToListAsync();
        }

        public async Task AddAsync(Achievement achievement)
        {
            await _context.Achievements.AddAsync(achievement);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Achievement achievement)
        {
            _context.Entry(achievement).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var achievement = await _context.Achievements.FindAsync(id);
            if (achievement != null)
            {
                _context.Achievements.Remove(achievement);
                await _context.SaveChangesAsync();
            }
        }
    }
}
