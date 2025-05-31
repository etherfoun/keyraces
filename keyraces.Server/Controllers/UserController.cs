using keyraces.Core.Interfaces;
using keyraces.Server.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace keyraces.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class UserController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<UserController> _logger;
        private readonly ITypingSessionService _typingSessionService;
        private readonly ITypingStatisticService _statisticService;

        public UserController(
            UserManager<IdentityUser> userManager,
            ILogger<UserController> logger,
            ITypingSessionService typingSessionService = null!,
            ITypingStatisticService statisticService = null!)
        {
            _userManager = userManager;
            _logger = logger;
            _typingSessionService = typingSessionService;
            _statisticService = statisticService;
        }

        [HttpGet]
        public async Task<ActionResult> GetAllUsers()
        {
            try
            {
                var users = await _userManager.Users.ToListAsync();
                var userDtos = users.Select(u => new UserDto
                {
                    Id = int.TryParse(u.Id, out var idValue) ? idValue : 0, // Populates int Id
                    UserId = u.Id, // Populates string UserId
                    Name = u.UserName ?? string.Empty,
                    Email = u.Email ?? string.Empty
                });

                return Ok(userDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user list");
                return StatusCode(500, new { message = "Error getting user list" });
            }
        }

        [HttpGet("{id}")] // This 'id' parameter is string, referring to IdentityUser.Id
        public async Task<ActionResult> GetUserById(string id)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                return Ok(new UserDto
                {
                    Id = int.TryParse(user.Id, out var idValue) ? idValue : 0, // Populates int Id
                    UserId = user.Id, // Populates string UserId
                    Name = user.UserName ?? string.Empty,
                    Email = user.Email ?? string.Empty
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting user with ID {id}");
                return StatusCode(500, new { message = "Error getting user" });
            }
        }

        [HttpGet("by-email")]
        public async Task<ActionResult> GetUserByEmail([FromQuery] string email)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                return Ok(new UserDto
                {
                    Id = int.TryParse(user.Id, out var idValue) ? idValue : 0, // Populates int Id
                    UserId = user.Id, // Populates string UserId
                    Name = user.UserName ?? string.Empty,
                    Email = user.Email ?? string.Empty
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting user with email {email}");
                return StatusCode(500, new { message = "Error getting user" });
            }
        }

        // This method still expects an int userId. If this refers to your DTO's int Id,
        // be aware it will likely be 0 if the user was identified by a GUID.
        // If it should use the string UserId, this signature and the service need to change.
        [HttpGet("{userId}/statistics")]
        [AllowAnonymous]
        public async Task<ActionResult> GetUserStatistics(int userId)
        {
            try
            {
                if (_typingSessionService == null || _statisticService == null)
                {
                    return StatusCode(500, new { message = "Statistics services are not configured" });
                }
                // If this 'userId' is meant to be the IdentityUser.Id, it should be string.
                // And _typingSessionService.GetByUserAsync should expect a string.
                var sessions = await _typingSessionService.GetByUserAsync(userId);

                var statistics = new List<TypingStatistic>();
                foreach (var session in sessions)
                {
                    try
                    {
                        var stat = await _statisticService.GetBySessionAsync(session.Id);
                        if (stat != null)
                        {
                            statistics.Add(stat);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to get statistics for session {SessionId}", session.Id);
                        continue;
                    }
                }
                return Ok(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting user statistics for int ID {userId}");
                return StatusCode(500, new { message = "Error getting user statistics" });
            }
        }
    }
}
