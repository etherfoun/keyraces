using Microsoft.AspNetCore.Identity;

namespace keyraces.Core.Interfaces
{
    public interface IRoleService
    {
        Task<bool> EnsureRoleExistsAsync(string roleName);
        Task<bool> IsUserInRoleAsync(string userId, string roleName);
        Task<bool> AddUserToRoleAsync(string userId, string roleName);
        Task<bool> RemoveUserFromRoleAsync(string userId, string roleName);
        Task<IEnumerable<string>> GetUserRolesAsync(string userId);
        Task<IEnumerable<IdentityUser>> GetUsersInRoleAsync(string roleName);
        Task<IEnumerable<IdentityRole>> GetAllRolesAsync();
    }
}
