using keyraces.Core.Entities;
using keyraces.Core.Interfaces;
using keyraces.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace keyraces.Infrastructure.Repositories
{
    public class CompetitionParticipantRepository : ICompetitionParticipantRepository
    {
        private readonly AppDbContext _ctx;
        public CompetitionParticipantRepository(AppDbContext ctx) => _ctx = ctx;
        public async Task<CompetitionParticipant> GetAsync(int compId, int userId) =>
            await _ctx.Participants.FindAsync(compId, userId);
        public async Task<IEnumerable<CompetitionParticipant>> ListByCompetitionAsync(int compId) =>
            await _ctx.Participants.Where(p => p.CompetitionId == compId).ToListAsync();
        public async Task AddAsync(CompetitionParticipant p) { _ctx.Participants.Add(p); await _ctx.SaveChangesAsync(); }
        public async Task UpdateAsync(CompetitionParticipant p) { _ctx.Participants.Update(p); await _ctx.SaveChangesAsync(); }
    }
}
