using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using keyraces.Core.Entities;
using keyraces.Core.Interfaces;
using keyraces.Infrastructure.Data;

namespace keyraces.Infrastructure.Repositories
{
    public class UserProfileRepository : IUserProfileRepository
    {
        private readonly AppDbContext _ctx;
        public UserProfileRepository(AppDbContext ctx) => _ctx = ctx;

        public Task<UserProfile> GetByIdentityIdAsync(string identityUserId) =>
            _ctx.UserProfiles
                .SingleOrDefaultAsync(up => up.IdentityUserId == identityUserId);

        public async Task AddAsync(UserProfile profile)
        {
            await _ctx.UserProfiles.AddAsync(profile);
            await _ctx.SaveChangesAsync();
        }

        public async Task UpdateAsync(UserProfile profile)
        {
            _ctx.UserProfiles.Update(profile);
            await _ctx.SaveChangesAsync();
        }
    }
}
