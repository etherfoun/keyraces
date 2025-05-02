using keyraces.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace keyraces.Core.Interfaces
{
    public interface ISessionRepository
    {
        Task<TypingSession> GetByIdAsync(int id);
        Task<IEnumerable<TypingSession>> ListByUserAsync(int userId);
        Task AddAsync(TypingSession session);
        Task UpdateAsync(TypingSession session);
    }
}
