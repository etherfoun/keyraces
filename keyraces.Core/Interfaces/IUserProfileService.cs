using keyraces.Core.Entities;

namespace keyraces.Core.Interfaces
{
    public interface IUserProfileService
    {
        Task<UserProfile> GetOrCreateAsync(string identityUserId, string name);
        Task<UserProfile> GetByIdentityIdAsync(string identityUserId);
        Task<UserProfile> GetByIdAsync(int id);
        Task<IEnumerable<UserProfile>> GetAllAsync();
        Task UpdateAsync(UserProfile profile);
        Task CreateProfileAsync(string identityUserId, string name);
    }
}
