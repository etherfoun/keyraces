using keyraces.Core.Interfaces;
using keyraces.Server.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace keyraces.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class RoleController : ControllerBase
    {
        private readonly IRoleService _roleService;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<RoleController> _logger;

        public RoleController(
            IRoleService roleService,
            UserManager<IdentityUser> userManager,
            ILogger<RoleController> logger)
        {
            _roleService = roleService;
            _userManager = userManager;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<RoleDto>>> GetAllRoles()
        {
            try
            {
                var roles = await _roleService.GetAllRolesAsync();
                return Ok(roles.Select(r => new RoleDto { Id = r.Id, Name = r.Name }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting the list of roles");
                return StatusCode(500, new { message = "Error while getting the list of roles" });
            }
        }

        [HttpGet("users/{roleName}")]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetUsersInRole(string roleName)
        {
            try
            {
                var users = await _roleService.GetUsersInRoleAsync(roleName);
                return Ok(users.Select(u => new UserDto
                {
                    Id = int.TryParse(u.Id, out var idValue) ? idValue : 0,
                    UserId = u.Id,
                    Name = u.UserName ?? string.Empty,
                    Email = u.Email ?? string.Empty
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error while getting users in role {roleName}");
                return StatusCode(500, new { message = $"Error while getting users in role {roleName}" });
            }
        }

        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<string>>> GetUserRoles(string userId)
        {
            try
            {
                _logger.LogInformation($"RoleController: GetUserRoles for UserId: '{userId}'");
                var roles = await _roleService.GetUserRolesAsync(userId);
                return Ok(roles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error while getting user roles {userId}");
                return StatusCode(500, new { message = $"Error while getting user roles" });
            }
        }

        [HttpPost]
        public async Task<ActionResult> CreateRole([FromBody] CreateRoleDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Name))
                {
                    return BadRequest(new { message = "Name of role cannot be empty" });
                }

                var result = await _roleService.EnsureRoleExistsAsync(dto.Name);
                if (result)
                {
                    return Ok(new { message = $"Role {dto.Name} successfully created or already exists" });
                }
                else
                {
                    return StatusCode(500, new { message = $"Failed to create role {dto.Name}" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating role {dto.Name}");
                return StatusCode(500, new { message = "Error creating role" });
            }
        }

        [HttpPost("user")]
        public async Task<ActionResult> AddUserToRole([FromBody] UserRoleDto dto)
        {
            try
            {
                _logger.LogInformation($"RoleController: AddUserToRole - Received UserId: '{dto.UserId}', RoleName: '{dto.RoleName}'");

                if (string.IsNullOrWhiteSpace(dto.UserId) || string.IsNullOrWhiteSpace(dto.RoleName))
                {
                    _logger.LogWarning("RoleController: AddUserToRole - UserId or RoleName is null or whitespace.");
                    return BadRequest(new { message = "User ID and role name cannot be empty" });
                }

                var result = await _roleService.AddUserToRoleAsync(dto.UserId, dto.RoleName);
                if (result)
                {
                    return Ok(new { message = $"User successfully added to role {dto.RoleName}" });
                }
                else
                {
                    _logger.LogWarning($"RoleController: AddUserToRole - _roleService.AddUserToRoleAsync for user '{dto.UserId}' and role '{dto.RoleName}' returned false.");
                    return StatusCode(500, new { message = $"Failed to add user to role {dto.RoleName}. Check server logs for RoleService details." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"RoleController: AddUserToRole - Error adding user '{dto.UserId}' to role '{dto.RoleName}'");
                return StatusCode(500, new { message = "Error adding user to role" });
            }
        }

        [HttpDelete("user")]
        public async Task<ActionResult> RemoveUserFromRole([FromQuery] string userId, [FromQuery] string roleName)
        {
            try
            {
                _logger.LogInformation($"RoleController: RemoveUserFromRole - UserId: '{userId}', RoleName: '{roleName}'");
                if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(roleName))
                {
                    _logger.LogWarning("RoleController: RemoveUserFromRole - UserId or RoleName is null or whitespace.");
                    return BadRequest(new { message = "User ID and role name cannot be empty" });
                }

                var result = await _roleService.RemoveUserFromRoleAsync(userId, roleName);
                if (result)
                {
                    return Ok(new { message = $"User successfully removed from role {roleName}" });
                }
                else
                {
                    _logger.LogWarning($"RoleController: RemoveUserFromRole - _roleService.RemoveUserFromRoleAsync for user '{userId}' and role '{roleName}' returned false.");
                    return StatusCode(500, new { message = $"Failed to remove user from role {roleName}" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"RoleController: RemoveUserFromRole - Error removing user '{userId}' from role '{roleName}'");
                return StatusCode(500, new { message = "Error removing user from role" });
            }
        }

        [HttpGet("check-admin")]
        [AllowAnonymous]
        public async Task<ActionResult> CheckAdminRole()
        {
            try
            {
                var adminRoleExists = await _roleService.EnsureRoleExistsAsync("Admin");

                if (!adminRoleExists)
                {
                    _logger.LogInformation("Admin role does not exist");
                    return Ok(new { adminExists = false, adminCount = 0, message = "Admin role does not exist" });
                }

                var admins = await _roleService.GetUsersInRoleAsync("Admin");
                var adminCount = admins.Count();
                var adminExists = admins.Any();

                _logger.LogInformation($"Admin check: exists = {adminExists}, count = {adminCount}");

                return Ok(new
                {
                    adminExists = adminExists,
                    adminCount = adminCount,
                    message = adminExists
                        ? $"Found {adminCount} administrators"
                        : "Administrators not found"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking Admin role");
                return StatusCode(500, new { message = "Error checking Admin role" });
            }
        }

        [HttpPost("setup-admin")]
        [AllowAnonymous]
        public async Task<ActionResult> SetupAdminRole([FromBody] SetupAdminDto dto)
        {
            try
            {
                _logger.LogInformation($"Attempting to set up administrator for email: {dto.AdminEmail}");

                var adminCheckResponse = await CheckAdminRole() as ObjectResult;
                if (adminCheckResponse?.Value != null)
                {
                    var jsonString = JsonSerializer.Serialize(adminCheckResponse.Value);
                    _logger.LogInformation($"Admin check result: {jsonString}");

                    var jsonDoc = JsonDocument.Parse(jsonString);
                    var root = jsonDoc.RootElement;

                    if (root.TryGetProperty("adminExists", out var adminExistsElement) &&
                        adminExistsElement.GetBoolean())
                    {
                        _logger.LogInformation("Administrators already exist");
                        return BadRequest(new { message = "Administrators already exist. Use an existing administrator account to manage roles." });
                    }
                }

                if (dto.SecretKey != "keyraces-admin-setup-2025")
                {
                    _logger.LogWarning("Invalid secret key entered");
                    return BadRequest(new { message = "Invalid secret key" });
                }

                var user = await _userManager.FindByEmailAsync(dto.AdminEmail);
                if (user == null)
                {
                    _logger.LogWarning($"User with email {dto.AdminEmail} not found");
                    return NotFound(new { message = $"User with email {dto.AdminEmail} not found" });
                }

                _logger.LogInformation($"User found: {user.Id}, {user.UserName}");

                var roleCreated = await _roleService.EnsureRoleExistsAsync("Admin");
                if (!roleCreated)
                {
                    _logger.LogError("Failed to create Admin role");
                    return StatusCode(500, new { message = "Failed to create Admin role" });
                }

                var result = await _roleService.AddUserToRoleAsync(user.Id, "Admin");
                if (result)
                {
                    _logger.LogInformation($"User {dto.AdminEmail} successfully assigned as administrator");
                    return Ok(new { message = $"User {dto.AdminEmail} successfully assigned as administrator" });
                }
                else
                {
                    _logger.LogError($"Failed to assign user {dto.AdminEmail} as administrator");
                    return StatusCode(500, new { message = "Failed to assign user as administrator" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting up Admin role");
                return StatusCode(500, new { message = $"Error setting up Admin role: {ex.Message}" });
            }
        }
    }
}
