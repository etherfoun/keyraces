using keyraces.Core.Entities;

namespace keyraces.Core.Interfaces
{
    public interface IUserProfileRepository
    {
        Task<UserProfile?> GetByIdentityIdAsync(string identityUserId);
        Task AddAsync(UserProfile profile);
        Task UpdateAsync(UserProfile profile);
    }
}
