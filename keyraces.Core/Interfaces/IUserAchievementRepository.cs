using keyraces.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace keyraces.Core.Interfaces
{
    public interface IUserAchievementRepository
    {
        Task<IEnumerable<UserAchievement>> ListByUserAsync(int userId);
        Task AddAsync(UserAchievement ua);
    }
}
