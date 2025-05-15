using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using keyraces.Core.Interfaces;
using keyraces.Server.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace keyraces.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")] // Только администраторы могут управлять ролями
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
                _logger.LogError(ex, "Ошибка при получении списка ролей");
                return StatusCode(500, new { message = "Ошибка при получении списка ролей" });
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
                    UserId = int.TryParse(u.Id, out var userIdValue) ? userIdValue : 0,
                    Name = u.UserName ?? string.Empty,
                    Email = u.Email ?? string.Empty
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при получении пользователей в роли {roleName}");
                return StatusCode(500, new { message = $"Ошибка при получении пользователей в роли {roleName}" });
            }
        }

        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<string>>> GetUserRoles(string userId)
        {
            try
            {
                var roles = await _roleService.GetUserRolesAsync(userId);
                return Ok(roles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при получении ролей пользователя {userId}");
                return StatusCode(500, new { message = $"Ошибка при получении ролей пользователя" });
            }
        }

        [HttpPost]
        public async Task<ActionResult> CreateRole([FromBody] CreateRoleDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Name))
                {
                    return BadRequest(new { message = "Имя роли не может быть пустым" });
                }

                var result = await _roleService.EnsureRoleExistsAsync(dto.Name);
                if (result)
                {
                    return Ok(new { message = $"Роль {dto.Name} успешно создана или уже существует" });
                }
                else
                {
                    return StatusCode(500, new { message = $"Не удалось создать роль {dto.Name}" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при создании роли {dto.Name}");
                return StatusCode(500, new { message = "Ошибка при создании роли" });
            }
        }

        [HttpPost("user")]
        public async Task<ActionResult> AddUserToRole([FromBody] UserRoleDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.UserId) || string.IsNullOrWhiteSpace(dto.RoleName))
                {
                    return BadRequest(new { message = "ID пользователя и имя роли не могут быть пустыми" });
                }

                var result = await _roleService.AddUserToRoleAsync(dto.UserId, dto.RoleName);
                if (result)
                {
                    return Ok(new { message = $"Пользователь успешно добавлен в роль {dto.RoleName}" });
                }
                else
                {
                    return StatusCode(500, new { message = $"Не удалось добавить пользователя в роль {dto.RoleName}" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при добавлении пользователя {dto.UserId} в роль {dto.RoleName}");
                return StatusCode(500, new { message = "Ошибка при добавлении пользователя в роль" });
            }
        }

        [HttpDelete("user")]
        public async Task<ActionResult> RemoveUserFromRole([FromQuery] string userId, [FromQuery] string roleName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(roleName))
                {
                    return BadRequest(new { message = "ID пользователя и имя роли не могут быть пустыми" });
                }

                var result = await _roleService.RemoveUserFromRoleAsync(userId, roleName);
                if (result)
                {
                    return Ok(new { message = $"Пользователь успешно удален из роли {roleName}" });
                }
                else
                {
                    return StatusCode(500, new { message = $"Не удалось удалить пользователя из роли {roleName}" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при удалении пользователя {userId} из роли {roleName}");
                return StatusCode(500, new { message = "Ошибка при удалении пользователя из роли" });
            }
        }

        [HttpGet("check-admin")]
        [AllowAnonymous] // Этот метод доступен всем для проверки статуса админа
        public async Task<ActionResult> CheckAdminRole()
        {
            try
            {
                // Проверяем, существует ли роль Admin
                var adminRoleExists = await _roleService.EnsureRoleExistsAsync("Admin");

                if (!adminRoleExists)
                {
                    _logger.LogInformation("Роль Admin не существует");
                    return Ok(new { adminExists = false, adminCount = 0, message = "Роль Admin не существует" });
                }

                // Проверяем, есть ли хотя бы один пользователь с ролью Admin
                var admins = await _roleService.GetUsersInRoleAsync("Admin");
                var adminCount = admins.Count();
                var adminExists = admins.Any();

                _logger.LogInformation($"Проверка админов: существуют = {adminExists}, количество = {adminCount}");

                return Ok(new
                {
                    adminExists = adminExists,
                    adminCount = adminCount,
                    message = adminExists
                        ? $"Найдено {adminCount} администраторов"
                        : "Администраторы не найдены"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при проверке роли Admin");
                return StatusCode(500, new { message = "Ошибка при проверке роли Admin" });
            }
        }

        [HttpPost("setup-admin")]
        [AllowAnonymous] // Этот метод доступен всем для первоначальной настройки
        public async Task<ActionResult> SetupAdminRole([FromBody] SetupAdminDto dto)
        {
            try
            {
                _logger.LogInformation($"Попытка настройки администратора для email: {dto.AdminEmail}");

                // Проверяем, существует ли уже роль Admin и есть ли в ней пользователи
                var adminCheckResponse = await CheckAdminRole() as ObjectResult;
                if (adminCheckResponse?.Value != null)
                {
                    // Преобразуем в строку JSON и обратно для безопасного доступа к свойствам
                    var jsonString = JsonSerializer.Serialize(adminCheckResponse.Value);
                    _logger.LogInformation($"Результат проверки админа: {jsonString}");

                    var jsonDoc = JsonDocument.Parse(jsonString);
                    var root = jsonDoc.RootElement;

                    if (root.TryGetProperty("adminExists", out var adminExistsElement) &&
                        adminExistsElement.GetBoolean())
                    {
                        _logger.LogInformation("Администраторы уже существуют");
                        return BadRequest(new { message = "Администраторы уже существуют. Используйте существующую учетную запись администратора для управления ролями." });
                    }
                }

                // Проверяем секретный ключ (простая защита для первоначальной настройки)
                if (dto.SecretKey != "keyraces-admin-setup-2025")
                {
                    _logger.LogWarning("Введен неверный секретный ключ");
                    return BadRequest(new { message = "Неверный секретный ключ" });
                }

                // Находим пользователя по email
                var user = await _userManager.FindByEmailAsync(dto.AdminEmail);
                if (user == null)
                {
                    _logger.LogWarning($"Пользователь с email {dto.AdminEmail} не найден");
                    return NotFound(new { message = $"Пользователь с email {dto.AdminEmail} не найден" });
                }

                _logger.LogInformation($"Пользователь найден: {user.Id}, {user.UserName}");

                // Создаем роль Admin, если она не существует
                var roleCreated = await _roleService.EnsureRoleExistsAsync("Admin");
                if (!roleCreated)
                {
                    _logger.LogError("Не удалось создать роль Admin");
                    return StatusCode(500, new { message = "Не удалось создать роль Admin" });
                }

                // Добавляем пользователя в роль Admin
                var result = await _roleService.AddUserToRoleAsync(user.Id, "Admin");
                if (result)
                {
                    _logger.LogInformation($"Пользователь {dto.AdminEmail} успешно назначен администратором");
                    return Ok(new { message = $"Пользователь {dto.AdminEmail} успешно назначен администратором" });
                }
                else
                {
                    _logger.LogError($"Не удалось назначить пользователя {dto.AdminEmail} администратором");
                    return StatusCode(500, new { message = "Не удалось назначить пользователя администратором" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при настройке роли Admin");
                return StatusCode(500, new { message = $"Ошибка при настройке роли Admin: {ex.Message}" });
            }
        }
    }
}
