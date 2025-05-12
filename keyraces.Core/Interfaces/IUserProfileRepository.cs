using keyraces.Core.Entities;

namespace keyraces.Core.Interfaces
{
    public interface IUserProfileRepository
    {
        Task<UserProfile?> FindByIdentityIdAsync(string identityUserId);
        Task<UserProfile?> GetByIdentityIdAsync(string identityUserId);
        Task<UserProfile?> GetByIdAsync(int id);
        Task<IEnumerable<UserProfile>> ListAllAsync();
        Task AddAsync(UserProfile profile);
        Task UpdateAsync(UserProfile profile);
    }
}
