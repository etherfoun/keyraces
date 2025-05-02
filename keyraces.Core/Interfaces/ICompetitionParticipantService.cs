using keyraces.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
