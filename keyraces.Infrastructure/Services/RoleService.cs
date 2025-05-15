using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
                    throw new ArgumentException("Имя роли не может быть пустым", nameof(roleName));
                }

                // Проверяем, существует ли уже роль
                var roleExists = await _roleManager.RoleExistsAsync(roleName);
                if (!roleExists)
                {
                    // Создаем роль, если она не существует
                    var result = await _roleManager.CreateAsync(new IdentityRole(roleName));
                    if (!result.Succeeded)
                    {
                        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                        _logger.LogError($"Ошибка при создании роли {roleName}: {errors}");
                        return false;
                    }
                    _logger.LogInformation($"Роль {roleName} успешно создана");
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при проверке/создании роли {roleName}");
                return false;
            }
        }

        public async Task<bool> IsUserInRoleAsync(string userId, string roleName)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning($"Пользователь с ID {userId} не найден");
                    return false;
                }

                return await _userManager.IsInRoleAsync(user, roleName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при проверке роли {roleName} для пользователя {userId}");
                return false;
            }
        }

        public async Task<bool> AddUserToRoleAsync(string userId, string roleName)
        {
            try
            {
                // Убедимся, что роль существует
                if (!await EnsureRoleExistsAsync(roleName))
                {
                    return false;
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning($"Пользователь с ID {userId} не найден");
                    return false;
                }

                // Проверяем, есть ли у пользователя уже эта роль
                if (await _userManager.IsInRoleAsync(user, roleName))
                {
                    _logger.LogInformation($"Пользователь {user.UserName} уже имеет роль {roleName}");
                    return true;
                }

                // Добавляем пользователя в роль
                var result = await _userManager.AddToRoleAsync(user, roleName);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogError($"Ошибка при добавлении пользователя {user.UserName} в роль {roleName}: {errors}");
                    return false;
                }

                _logger.LogInformation($"Пользователь {user.UserName} успешно добавлен в роль {roleName}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при добавлении пользователя {userId} в роль {roleName}");
                return false;
            }
        }

        public async Task<bool> RemoveUserFromRoleAsync(string userId, string roleName)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning($"Пользователь с ID {userId} не найден");
                    return false;
                }

                // Проверяем, есть ли у пользователя эта роль
                if (!await _userManager.IsInRoleAsync(user, roleName))
                {
                    _logger.LogInformation($"Пользователь {user.UserName} не имеет роли {roleName}");
                    return true;
                }

                // Удаляем пользователя из роли
                var result = await _userManager.RemoveFromRoleAsync(user, roleName);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogError($"Ошибка при удалении пользователя {user.UserName} из роли {roleName}: {errors}");
                    return false;
                }

                _logger.LogInformation($"Пользователь {user.UserName} успешно удален из роли {roleName}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при удалении пользователя {userId} из роли {roleName}");
                return false;
            }
        }

        public async Task<IEnumerable<string>> GetUserRolesAsync(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning($"Пользователь с ID {userId} не найден");
                    return Enumerable.Empty<string>();
                }

                return await _userManager.GetRolesAsync(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при получении ролей пользователя {userId}");
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
                _logger.LogError(ex, $"Ошибка при получении пользователей в роли {roleName}");
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
                _logger.LogError(ex, "Ошибка при получении всех ролей");
                return Enumerable.Empty<IdentityRole>();
            }
        }
    }
}
