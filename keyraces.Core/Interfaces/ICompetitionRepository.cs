using keyraces.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace keyraces.Core.Interfaces
{
    public interface ICompetitionRepository
    {
        Task<Competition> GetByIdAsync(int id);
        Task<IEnumerable<Competition>> ListAsync();
        Task AddAsync(Competition competition);
        Task UpdateAsync(Competition competition);
    }
}
