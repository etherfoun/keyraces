using keyraces.Core.Entities;

namespace keyraces.Core.Interfaces
{
    public interface IUserProfileService
    {
        Task<UserProfile> CreateProfileAsync(string identityUserId, string name);

        Task<UserProfile> GetOrCreateAsync(string identityUserId, string fallBackName);

        Task UpdateNameAsync(string identityUserId, string newName);
    }
}
