using keyraces.Core.Entities;
using keyraces.Core.Interfaces;
using keyraces.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace keyraces.Infrastructure.Repositories
{
    public class CompetitionRepository : ICompetitionRepository
    {
        private readonly AppDbContext _ctx;
        public CompetitionRepository(AppDbContext ctx) => _ctx = ctx;

        public async Task<Competition> GetByIdAsync(int id)
        {
            var competition = await _ctx.Competitions.FindAsync(id);
            if (competition == null)
            {
                throw new InvalidOperationException($"Competition with ID {id} not found.");
            }
            return competition;
        }

        public async Task<IEnumerable<Competition>> ListAsync() => await _ctx.Competitions.ToListAsync();

        public async Task AddAsync(Competition competition)
        {
            _ctx.Competitions.Add(competition);
            await _ctx.SaveChangesAsync();
        }

        public async Task UpdateAsync(Competition competition)
        {
            _ctx.Competitions.Update(competition);
            await _ctx.SaveChangesAsync();
        }
    }
}
