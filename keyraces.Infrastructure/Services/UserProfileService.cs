using keyraces.Core.Entities;
using keyraces.Core.Interfaces;

namespace keyraces.Infrastructure.Services
{
    public class UserProfileService : IUserProfileService
    {
        private readonly IUserProfileRepository _repo;

        public UserProfileService(IUserProfileRepository repo)
        {
            _repo = repo;
        }

        public Task<UserProfile> GetProfileAsync(string identityUserId) =>
            _repo.GetByIdentityIdAsync(identityUserId);

        public async Task<UserProfile> CreateProfileAsync(string identityUserId, string name)
        {
            var profile = new UserProfile(identityUserId, name);
            await _repo.AddAsync(profile);
            return profile;
        }

        public async Task UpdateNameAsync(string identityUserId, string newName)
        {
            var profile = await _repo.GetByIdentityIdAsync(identityUserId);
            if (profile == null)
                throw new KeyNotFoundException($"Profile for user {identityUserId} not found.");

            profile.Update(newName);
            await _repo.UpdateAsync(profile);
        }
    }
}
