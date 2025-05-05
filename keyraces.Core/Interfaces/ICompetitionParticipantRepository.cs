using keyraces.Core.Entities;

namespace keyraces.Core.Interfaces
{
    public interface ICompetitionParticipantRepository
    {
        Task<CompetitionParticipant> GetAsync(int competitionId, int userId);
        Task<IEnumerable<CompetitionParticipant>> ListByCompetitionAsync(int competitionId);
        Task AddAsync(CompetitionParticipant participant);
        Task UpdateAsync(CompetitionParticipant participant);
    }
}
