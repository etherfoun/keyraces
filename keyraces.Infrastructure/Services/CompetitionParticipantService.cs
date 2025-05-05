using keyraces.Core.Entities;
using keyraces.Core.Interfaces;

namespace keyraces.Infrastructure.Services
{
    public class CompetitionParticipantService : ICompetitionParticipantService
    {
        private readonly ICompetitionParticipantRepository _repo;
        public CompetitionParticipantService(ICompetitionParticipantRepository repo) => _repo = repo;
        public async Task JoinAsync(int compId, int userId)
        {
            var participant = new CompetitionParticipant(compId, userId);
            await _repo.AddAsync(participant);
        }
        public async Task BeginTypingAsync(int compId, int userId)
        {
            var p = await _repo.GetAsync(compId, userId);
            p.Begin();
            await _repo.UpdateAsync(p);
        }
        public async Task CompleteTypingAsync(int compId, int userId, double wpm, int errors)
        {
            var p = await _repo.GetAsync(compId, userId);
            p.Complete(wpm, errors);
            await _repo.UpdateAsync(p);
        }
        public Task<IEnumerable<CompetitionParticipant>> GetParticipantsAsync(int compId) =>
            _repo.ListByCompetitionAsync(compId);
    }
}
