using keyraces.Core.Entities;
using keyraces.Core.Interfaces;
using keyraces.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace keyraces.Infrastructure.Repositories
{
    public class SessionRepository : ISessionRepository
    {
        private readonly AppDbContext _ctx;
        public SessionRepository(AppDbContext ctx) => _ctx = ctx;

        public async Task<TypingSession> GetByIdAsync(int id)
        {
            var session = await _ctx.Sessions.FindAsync(id);
            if (session == null)
            {
                throw new InvalidOperationException($"TextSnippet with ID {id} not found.");
            }
            return session;
        }

        public async Task<IEnumerable<TypingSession>> ListByUserAsync(int userId) =>
            await _ctx.Sessions
                       .Where(s => s.UserId == userId)
                       .ToListAsync();

        public async Task AddAsync(TypingSession session)
        {
            _ctx.Sessions.Add(session);
            await _ctx.SaveChangesAsync();
        }

        public async Task UpdateAsync(TypingSession session)
        {
            _ctx.Sessions.Update(session);
            await _ctx.SaveChangesAsync();
        }
    }
}
