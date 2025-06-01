using keyraces.Core.Dtos;
using keyraces.Core.Entities;
using keyraces.Core.Interfaces;
using keyraces.Server.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace keyraces.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SessionController : ControllerBase
    {
        private readonly ITypingSessionService _sessionService;
        private readonly IAchievementCheckerService _achievementCheckerService;
        private readonly ILogger<SessionController> _logger;

        public SessionController(
            ITypingSessionService sessionService,
            IAchievementCheckerService achievementCheckerService,
            ILogger<SessionController> logger)
        {
            _sessionService = sessionService;
            _achievementCheckerService = achievementCheckerService;
            _logger = logger;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var session = await _sessionService.GetByIdAsync(id);
                if (session == null)
                {
                    return NotFound(new { message = $"Session with ID {id} not found" });
                }
                return Ok(session);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting session with ID {SessionId}", id);
                return StatusCode(500, new { message = "Error getting session" });
            }
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetByUser(int userId)
        {
            try
            {
                var sessions = await _sessionService.GetByUserAsync(userId);
                return Ok(sessions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user sessions {UserId}", userId);
                return StatusCode(500, new { message = "Error getting user sessions" });
            }
        }

        [HttpPost("complete")]
        [Authorize]
        public async Task<IActionResult> CompleteSession([FromBody] SessionCompleteRequestDto requestDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(identityUserId))
            {
                return Unauthorized(new { message = "User is not authenticated." });
            }

            try
            {
                _logger.LogInformation("Starting session completion for user {IdentityUserId}, session {SessionId}",
                    identityUserId, requestDto.SessionId);
                _logger.LogInformation("Session data: WPM={WPM}, Accuracy={Accuracy}, Errors={Errors}, Duration={Duration}",
                    requestDto.Wpm, requestDto.Accuracy, requestDto.Errors, requestDto.Duration);

                var sessionResultEntity = await _sessionService.FinalizeAndGetResultAsync(identityUserId, requestDto);

                if (sessionResultEntity == null)
                {
                    _logger.LogWarning("Session completion failed or did not return a TypingSessionResult entity for user {IdentityUserId} with DTO data: {@RequestDto}", identityUserId, requestDto);
                    return BadRequest(new { message = "Failed to complete session or retrieve session result data." });
                }

                _logger.LogInformation("Session result created: ID={Id}, WPM={WPM}, Accuracy={Accuracy}",
                    sessionResultEntity.Id, sessionResultEntity.WPM, sessionResultEntity.Accuracy);

                _logger.LogInformation("Calling achievement checker for user {IdentityUserId}, session result {SessionResultId}",
                    identityUserId, sessionResultEntity.Id);

                await _achievementCheckerService.CheckAndAwardAchievementsAfterSessionAsync(identityUserId, sessionResultEntity);

                _logger.LogInformation("Session {SessionId} completed successfully for user {IdentityUserId}. TypingSessionResult ID: {TypingSessionResultId}. Achievements checked.",
                    requestDto.SessionId, identityUserId, sessionResultEntity.Id);

                return Ok(sessionResultEntity);
            }
            catch (ArgumentException argEx)
            {
                _logger.LogWarning(argEx, "Argument error during session completion for user {IdentityUserId} with DTO: {@RequestDto}", identityUserId, requestDto);
                return BadRequest(new { message = argEx.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing session for user {IdentityUserId} with DTO: {@RequestDto}", identityUserId, requestDto);
                return StatusCode(500, new { message = "An unexpected error occurred while completing the session." });
            }
        }
    }
}
