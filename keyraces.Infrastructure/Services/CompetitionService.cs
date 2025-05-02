using keyraces.Core.Entities;
using keyraces.Core.Interfaces;

namespace keyraces.Infrastructure.Services
{
    public class CompetitionService : ICompetitionService
    {
        private readonly ICompetitionRepository _repo;
        public CompetitionService(ICompetitionRepository repo) => _repo = repo;
        public async Task<Competition> CreateAsync(string title, int snippetId, DateTime startTime)
        {
            var comp = new Competition(title, snippetId, startTime);
            await _repo.AddAsync(comp);
            return comp;
        }
        public async Task StartAsync(int id)
        {
            var comp = await _repo.GetByIdAsync(id);
            comp.Start();
            await _repo.UpdateAsync(comp);
        }
        public async Task FinishAsync(int id)
        {
            var comp = await _repo.GetByIdAsync(id);
            comp.Finish();
            await _repo.UpdateAsync(comp);
        }
        public Task<IEnumerable<Competition>> GetAllAsync() => _repo.ListAsync();
        public Task<Competition> GetByIdAsync(int competitionId)
    => _repo.GetByIdAsync(competitionId);
    }
}
