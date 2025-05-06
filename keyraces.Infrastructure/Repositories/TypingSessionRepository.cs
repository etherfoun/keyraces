using Microsoft.EntityFrameworkCore;
using keyraces.Core.Entities;
using keyraces.Core.Interfaces;
using keyraces.Infrastructure.Data;

namespace keyraces.Infrastructure.Repositories
{
    public class TypingSessionRepository : ITypingSessionRepository
    {
        private readonly AppDbContext _ctx;
        public TypingSessionRepository(AppDbContext ctx)
            => _ctx = ctx;

        public async Task<TypingSession> AddAsync(TypingSession entity)
        {
            await _ctx.Sessions.AddAsync(entity);
            await _ctx.SaveChangesAsync();
            return entity;
        }

        public async Task<TypingSession?> GetByIdAsync(int id)
            => await _ctx.Sessions.FindAsync(id);

        public async Task<IEnumerable<TypingSession>> ListByUserAsync(int userId)
            => await _ctx.Sessions
                         .Where(s => s.UserId == userId)
                         .ToListAsync();

        public async Task UpdateAsync(TypingSession entity)
        {
            _ctx.Sessions.Update(entity);
            await _ctx.SaveChangesAsync();
        }
    }
}
