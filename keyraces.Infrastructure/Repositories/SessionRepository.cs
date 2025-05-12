using keyraces.Core.Entities;
using keyraces.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace keyraces.Infrastructure.Repositories
{
    public class SessionRepository : ISessionRepository
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<SessionRepository> _logger;
        private const string UserIdKey = "CurrentUserId";
        private const string UsernameKey = "CurrentUsername";

        public SessionRepository(IHttpContextAccessor httpContextAccessor, ILogger<SessionRepository> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task<TypingSession> GetByIdAsync(int id)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<TypingSession>> ListByUserAsync(int userId)
        {
            throw new NotImplementedException();
        }

        public async Task AddAsync(TypingSession session)
        {
            throw new NotImplementedException();
        }

        public async Task UpdateAsync(TypingSession session)
        {
            throw new NotImplementedException();
        }

        public int? GetCurrentUserId()
        {
            try
            {
                var session = _httpContextAccessor.HttpContext?.Session;
                if (session == null)
                {
                    _logger.LogWarning("Session is null");
                    return null;
                }

                var userIdString = session.GetString(UserIdKey);
                if (string.IsNullOrEmpty(userIdString))
                {
                    _logger.LogWarning("UserId not found in session");
                    return null;
                }

                if (int.TryParse(userIdString, out int userId))
                {
                    return userId;
                }
                else
                {
                    _logger.LogWarning("Failed to parse UserId from session: {UserIdString}", userIdString);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user ID from session");
                return null;
            }
        }

        public string? GetCurrentUsername()
        {
            try
            {
                var session = _httpContextAccessor.HttpContext?.Session;
                if (session == null)
                {
                    _logger.LogWarning("Session is null");
                    return null;
                }

                var username = session.GetString(UsernameKey);
                if (string.IsNullOrEmpty(username))
                {
                    _logger.LogWarning("Username not found in session");
                    return null;
                }

                return username;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current username from session");
                return null;
            }
        }

        public async Task ClearSessionAsync()
        {
            try
            {
                var session = _httpContextAccessor.HttpContext?.Session;
                if (session != null)
                {
                    session.Remove(UserIdKey);
                    session.Remove(UsernameKey);
                    _logger.LogInformation("Session cleared");
                }
                else
                {
                    _logger.LogWarning("Session is null, cannot clear");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing session");
            }
        }

        public async Task ClearCurrentUserAsync()
        {
            await ClearSessionAsync();
        }

        public async Task SetCurrentUserAsync(int userId, string username)
        {
            try
            {
                var session = _httpContextAccessor.HttpContext?.Session;
                if (session != null)
                {
                    session.SetString(UserIdKey, userId.ToString());
                    session.SetString(UsernameKey, username);
                    _logger.LogInformation("SetCurrentUserAsync: Set user ID {UserId} and username {Username} in session", userId, username);
                }
                else
                {
                    _logger.LogWarning("Session is null, cannot set user data");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting current user in session");
            }
        }
    }
}
