using keyraces.Core.Entities;
using System.Threading.Tasks;

namespace keyraces.Core.Interfaces
{
    public interface IAchievementCheckerService
    {
        Task CheckAndAwardAchievementsAfterSessionAsync(string identityUserId, TypingSessionResult sessionResult);
    }
}
