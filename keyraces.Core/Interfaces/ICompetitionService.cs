using keyraces.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace keyraces.Core.Interfaces
{
    public interface ICompetitionService
    {
        Task<Competition> CreateAsync(string title, int snippetId, DateTime startTime);
        Task StartAsync(int competitionId);
        Task FinishAsync(int competitionId);
        Task<IEnumerable<Competition>> GetAllAsync();
        Task<Competition> GetByIdAsync(int competitionId);
    }
}
