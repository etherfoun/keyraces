using keyraces.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace keyraces.Core.Interfaces
{
    public interface ITypingSessionService
    {
        Task<TypingSession> StartSessionAsync(int userId, int textSnippetId);
        Task CompleteSessionAsync(int sessionId, DateTime endTime);
        Task<IEnumerable<TypingSession>> GetByUserAsync(int userId);
    }
}
