using System;
using System.Threading.Tasks;
using keyraces.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace keyraces.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SessionController : ControllerBase
    {
        private readonly ITypingSessionService _sessionService;
        private readonly ILogger<SessionController> _logger;

        public SessionController(
            ITypingSessionService sessionService,
            ILogger<SessionController> logger)
        {
            _sessionService = sessionService;
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
                    return NotFound(new { message = $"Сессия с ID {id} не найдена" });
                }
                return Ok(session);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении сессии с ID {SessionId}", id);
                return StatusCode(500, new { message = "Ошибка при получении сессии" });
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
                _logger.LogError(ex, "Ошибка при получении сессий пользователя {UserId}", userId);
                return StatusCode(500, new { message = "Ошибка при получении сессий пользователя" });
            }
        }
    }
}
