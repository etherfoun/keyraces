using keyraces.Core.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace keyraces.Infrastructure.Services
{
    public class RoleService : IRoleService
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<RoleService> _logger;

        public RoleService(
            RoleManager<IdentityRole> roleManager,
            UserManager<IdentityUser> userManager,
            ILogger<RoleService> logger)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<bool> EnsureRoleExistsAsync(string roleName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(roleName))
                {
                    throw new ArgumentException("Role name cannot be empty", nameof(roleName));
                }

                var roleExists = await _roleManager.RoleExistsAsync(roleName);
                if (!roleExists)
                {
                    var result = await _roleManager.CreateAsync(new IdentityRole(roleName));
                    if (!result.Succeeded)
                    {
                        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                        _logger.LogError($"Error while creating a role {roleName}: {errors}");
                        return false;
                    }
                    _logger.LogInformation($"Role {roleName} created successfully");
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error while checking/creating a role {roleName}");
                return false;
            }
        }

        public async Task<bool> IsUserInRoleAsync(string userId, string roleName)
        {
            try
            {
                _logger.LogInformation($"RoleService: IsUserInRoleAsync - Checking role '{roleName}' for user ID '{userId}'");
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning($"RoleService: IsUserInRoleAsync - User with ID '{userId}' not found");
                    return false;
                }

                return await _userManager.IsInRoleAsync(user, roleName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error while checking role {roleName} for user {userId}");
                return false;
            }
        }

        public async Task<bool> AddUserToRoleAsync(string userId, string roleName)
        {
            try
            {
                _logger.LogInformation($"RoleService: AddUserToRoleAsync - Attempting to add user ID '{userId}' to role '{roleName}'");

                if (!await EnsureRoleExistsAsync(roleName))
                {
                    _logger.LogWarning($"RoleService: AddUserToRoleAsync - Role '{roleName}' does not exist or could not be created for user ID '{userId}'.");
                    return false;
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning($"RoleService: AddUserToRoleAsync - User with ID '{userId}' not found.");
                    return false;
                }

                if (await _userManager.IsInRoleAsync(user, roleName))
                {
                    _logger.LogInformation($"RoleService: AddUserToRoleAsync - User {user.UserName} already has role {roleName}");
                    return true;
                }

                var result = await _userManager.AddToRoleAsync(user, roleName);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogError($"RoleService: AddUserToRoleAsync - Error while adding user {user.UserName} to role {roleName}: {errors}");
                    return false;
                }

                _logger.LogInformation($"RoleService: AddUserToRoleAsync - User {user.UserName} added to role {roleName}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"RoleService: AddUserToRoleAsync - Error while adding user ID '{userId}' to role '{roleName}'");
                return false;
            }
        }

        public async Task<bool> RemoveUserFromRoleAsync(string userId, string roleName)
        {
            try
            {
                _logger.LogInformation($"RoleService: RemoveUserFromRoleAsync - Attempting to remove user ID '{userId}' from role '{roleName}'");
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning($"RoleService: RemoveUserFromRoleAsync - User with ID '{userId}' not found");
                    return false;
                }

                if (!await _userManager.IsInRoleAsync(user, roleName))
                {
                    _logger.LogInformation($"RoleService: RemoveUserFromRoleAsync - User {user.UserName} doesnt have a role {roleName}");
                    return true; // Or false, depending on desired behavior if user is not in role
                }

                var result = await _userManager.RemoveFromRoleAsync(user, roleName);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogError($"RoleService: RemoveUserFromRoleAsync - Error while deleting user {user.UserName} from role {roleName}: {errors}");
                    return false;
                }

                _logger.LogInformation($"RoleService: RemoveUserFromRoleAsync - User {user.UserName} successfully deleted from role {roleName}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"RoleService: RemoveUserFromRoleAsync - Error while deleting a user {userId} from role {roleName}");
                return false;
            }
        }

        public async Task<IEnumerable<string>> GetUserRolesAsync(string userId)
        {
            try
            {
                _logger.LogInformation($"RoleService: GetUserRolesAsync - Getting roles for user ID '{userId}'");
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning($"RoleService: GetUserRolesAsync - User with ID '{userId}' not found");
                    return Enumerable.Empty<string>();
                }

                return await _userManager.GetRolesAsync(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting user roles {userId}");
                return Enumerable.Empty<string>();
            }
        }

        public async Task<IEnumerable<IdentityUser>> GetUsersInRoleAsync(string roleName)
        {
            try
            {
                return await _userManager.GetUsersInRoleAsync(roleName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting users in role {roleName}");
                return Enumerable.Empty<IdentityUser>();
            }
        }

        public async Task<IEnumerable<IdentityRole>> GetAllRolesAsync()
        {
            try
            {
                return await _roleManager.Roles.ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all roles");
                return Enumerable.Empty<IdentityRole>();
            }
        }
    }
}
