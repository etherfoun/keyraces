using keyraces.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
