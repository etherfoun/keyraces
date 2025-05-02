using keyraces.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace keyraces.Core.Interfaces
{
    public interface ITypingStatisticService
    {
        Task<TypingStatistic> CreateAsync(int userId, int sessionId, double wpm, double accuracy);
        Task UpdateAsync(int id, double newWpm, double newAccuracy);
        Task<TypingStatistic> GetBySessionAsync(int sessionId);
    }
}
