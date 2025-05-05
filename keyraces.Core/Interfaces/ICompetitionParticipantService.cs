using keyraces.Core.Entities;

namespace keyraces.Core.Interfaces
{
    public interface ICompetitionParticipantService
    {
        Task JoinAsync(int competitionId, int userId);
        Task BeginTypingAsync(int competitionId, int userId);
        Task CompleteTypingAsync(int competitionId, int userId, double wpm, int errors);
        Task<IEnumerable<CompetitionParticipant>> GetParticipantsAsync(int competitionId);
    }
}
