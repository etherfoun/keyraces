using Microsoft.EntityFrameworkCore;
using keyraces.Core.Entities;
using keyraces.Core.Interfaces;
using keyraces.Infrastructure.Data;

namespace keyraces.Infrastructure.Repositories
{
    public class UserProfileRepository : IUserProfileRepository
    {
        private readonly IDbContextFactory<AppDbContext> _factory;
        public UserProfileRepository(IDbContextFactory<AppDbContext> factory) => _factory = factory;

        public async Task<UserProfile?> FindByIdentityIdAsync(string identityUserId)
        {
            await using var _ctx = _factory.CreateDbContext();
            return await _ctx.UserProfiles.SingleOrDefaultAsync(p => p.IdentityUserId == identityUserId);
        }

        public async Task<UserProfile?> GetByIdentityIdAsync(string identityUserId)
        {
            await using var _ctx = _factory.CreateDbContext();
            return await _ctx.UserProfiles
                       .SingleOrDefaultAsync(p => p.IdentityUserId == identityUserId);
        }

        public async Task AddAsync(UserProfile profile)
        {
            await using var _ctx = _factory.CreateDbContext();
            await _ctx.UserProfiles.AddAsync(profile);
            await _ctx.SaveChangesAsync();
        }

        public async Task UpdateAsync(UserProfile profile)
        {
            await using var _ctx = _factory.CreateDbContext();
            _ctx.UserProfiles.Update(profile);
            await _ctx.SaveChangesAsync();
        }
    }
}
