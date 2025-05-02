using keyraces.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace keyraces.Core.Interfaces
{
    public interface IAchievementRepository
    {
        Task<Achievement> GetByIdAsync(int id);
        Task<IEnumerable<Achievement>> ListAsync();
        Task AddAsync(Achievement achievement);
        Task UpdateAsync(Achievement achievement);
    }
}
