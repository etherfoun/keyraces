using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using keyraces.Core.Entities;
using keyraces.Core.Interfaces;
using keyraces.Server.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace keyraces.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")] // Только администраторы могут управлять пользователями
    public class UserController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<UserController> _logger;
        private readonly ITypingSessionService _typingSessionService;
        private readonly ITypingStatisticService _statisticService;

        public UserController(
            UserManager<IdentityUser> userManager,
            ILogger<UserController> logger,
            ITypingSessionService typingSessionService = null,
            ITypingStatisticService statisticService = null)
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
                    Id = int.TryParse(u.Id, out var idValue) ? idValue : 0,
                    UserId = int.TryParse(u.Id, out var userIdValue) ? userIdValue : 0,
                    Name = u.UserName ?? string.Empty,
                    Email = u.Email ?? string.Empty
                });

                return Ok(userDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении списка пользователей");
                return StatusCode(500, new { message = "Ошибка при получении списка пользователей" });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult> GetUserById(string id)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    return NotFound(new { message = "Пользователь не найден" });
                }

                return Ok(new UserDto
                {
                    Id = int.TryParse(user.Id, out var idValue) ? idValue : 0,
                    UserId = int.TryParse(user.Id, out var userIdValue) ? userIdValue : 0,
                    Name = user.UserName ?? string.Empty,
                    Email = user.Email ?? string.Empty
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при получении пользователя с ID {id}");
                return StatusCode(500, new { message = "Ошибка при получении пользователя" });
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
                    return NotFound(new { message = "Пользователь не найден" });
                }

                return Ok(new UserDto
                {
                    Id = int.TryParse(user.Id, out var idValue) ? idValue : 0,
                    UserId = int.TryParse(user.Id, out var userIdValue) ? userIdValue : 0,
                    Name = user.UserName ?? string.Empty,
                    Email = user.Email ?? string.Empty
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при получении пользователя с email {email}");
                return StatusCode(500, new { message = "Ошибка при получении пользователя" });
            }
        }

        [HttpGet("{userId}/statistics")]
        [AllowAnonymous] // Разрешаем доступ всем пользователям к своей статистике
        public async Task<ActionResult> GetUserStatistics(int userId)
        {
            try
            {
                if (_typingSessionService == null || _statisticService == null)
                {
                    return StatusCode(500, new { message = "Сервисы статистики не настроены" });
                }

                // Получаем все сессии пользователя
                var sessions = await _typingSessionService.GetByUserAsync(userId);

                // Получаем статистику для каждой сессии
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
                        _logger.LogWarning(ex, "Не удалось получить статистику для сессии {SessionId}", session.Id);
                        // Пропускаем сессии без статистики
                        continue;
                    }
                }

                return Ok(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при получении статистики пользователя {userId}");
                return StatusCode(500, new { message = "Ошибка при получении статистики пользователя" });
            }
        }
    }
}
