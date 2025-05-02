using keyraces.Core.Entities;

namespace keyraces.Core.Interfaces
{
    public interface IUserProfileService
    {
        Task<UserProfile> GetProfileAsync(string identityUserId);

        Task<UserProfile> CreateProfileAsync(string identityUserId, string name);

        Task UpdateNameAsync(string identityUserId, string newName);
    }
}
