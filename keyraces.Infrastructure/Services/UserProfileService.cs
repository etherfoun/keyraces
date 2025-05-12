using keyraces.Core.Entities;
using keyraces.Core.Interfaces;

namespace keyraces.Infrastructure.Services
{
    public class UserProfileService : IUserProfileService
    {
        private readonly IUserProfileRepository _repository;

        public UserProfileService(IUserProfileRepository repository)
        {
            _repository = repository;
        }

        public async Task<UserProfile> GetOrCreateAsync(string identityUserId, string name)
        {
            var existing = await _repository.FindByIdentityIdAsync(identityUserId);
            if (existing is not null)
                return existing;

            var profile = new UserProfile(identityUserId, name);
            await _repository.AddAsync(profile);
            return profile;
        }

        public async Task<UserProfile> GetByIdentityIdAsync(string identityUserId)
        {
            var profile = await _repository.GetByIdentityIdAsync(identityUserId);
            if (profile == null)
                throw new InvalidOperationException($"Profile for identity user {identityUserId} not found");

            return profile;
        }

        public async Task<UserProfile> GetByIdAsync(int id)
        {
            var profile = await _repository.GetByIdAsync(id);
            if (profile == null)
                throw new InvalidOperationException($"Profile with ID {id} not found");

            return profile;
        }

        public async Task<IEnumerable<UserProfile>> GetAllAsync()
        {
            return await _repository.ListAllAsync();
        }

        public async Task UpdateAsync(UserProfile profile)
        {
            await _repository.UpdateAsync(profile);
        }

        public async Task CreateProfileAsync(string identityUserId, string name)
        {
            var profile = new UserProfile(identityUserId, name);
            await _repository.AddAsync(profile);
        }
    }
}
