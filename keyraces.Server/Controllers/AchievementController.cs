using keyraces.Core.Entities;
using keyraces.Core.Interfaces;
using keyraces.Server.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace keyraces.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AchievementController : ControllerBase
    {
        private readonly IAchievementService _achievementService;
        private readonly IUserAchievementService _userAchievementService;
        private readonly IUserProfileService _userProfileService;
        private readonly ILogger<AchievementController> _logger;

        public AchievementController(
            IAchievementService achievementService,
            IUserAchievementService userAchievementService,
            IUserProfileService userProfileService,
            ILogger<AchievementController> logger)
        {
            _achievementService = achievementService;
            _userAchievementService = userAchievementService;
            _userProfileService = userProfileService;
            _logger = logger;
        }

        [HttpGet("definitions")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<Achievement>>> GetAchievementDefinitions()
        {
            try
            {
                _logger.LogInformation("Getting achievement definitions...");
                var achievements = await _achievementService.ListAsync();
                _logger.LogInformation("Retrieved {Count} achievements from service", achievements?.Count() ?? 0);

                if (achievements == null)
                {
                    _logger.LogWarning("AchievementService.ListAsync() returned null");
                    return Ok(new List<Achievement>());
                }

                var achievementList = achievements.ToList();
                _logger.LogInformation("Returning {Count} achievements: {Names}",
                    achievementList.Count,
                    string.Join(", ", achievementList.Select(a => a.Name)));

                return Ok(achievementList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting achievement definitions");
                return StatusCode(500, new { message = "Error retrieving achievements", error = ex.Message });
            }
        }

        [HttpGet("user")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<UserAchievementDisplayDto>>> GetUserAchievements()
        {
            var currentIdentityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentIdentityUserId))
            {
                return Unauthorized();
            }

            var currentUserProfile = await _userProfileService.GetByIdentityIdAsync(currentIdentityUserId);
            if (currentUserProfile == null)
            {
                return NotFound("User profile not found.");
            }

            var userAchievements = await _userAchievementService.GetUserAchievementsAsync(currentUserProfile.Id);
            if (userAchievements == null)
            {
                return Ok(Enumerable.Empty<UserAchievementDisplayDto>());
            }

            var dtos = userAchievements.Select(ua => new UserAchievementDisplayDto
            {
                AchievementId = ua.AchievementId,
                Key = ua.Achievement?.Key ?? Core.Enums.AchievementKey.Unknown,
                Name = ua.Achievement?.Name ?? "Unknown Achievement",
                Description = ua.Achievement?.Description ?? "Details unavailable.",
                AwardedAt = ua.AwardedAt,
                IconCssClass = ua.Achievement?.IconCssClass
            }).OrderByDescending(dto => dto.AwardedAt);

            return Ok(dtos);
        }

        [HttpPost("award/{achievementId}")]
        [Authorize]
        public async Task<IActionResult> AwardAchievementToCurrentUser(int achievementId)
        {
            var currentIdentityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentIdentityUserId))
            {
                return Unauthorized();
            }

            var currentUserProfile = await _userProfileService.GetByIdentityIdAsync(currentIdentityUserId);
            if (currentUserProfile == null)
            {
                return NotFound("User profile not found.");
            }

            try
            {
                await _userAchievementService.AwardAsync(currentUserProfile.Id, achievementId);
                return Ok($"Achievement {achievementId} awarded to user {currentUserProfile.Id}.");
            }
            catch (System.InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
            catch (System.ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
