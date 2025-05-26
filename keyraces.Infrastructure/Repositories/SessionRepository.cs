using keyraces.Core.Entities;
using keyraces.Core.Interfaces;
using keyraces.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace keyraces.Infrastructure.Repositories
{
    public class SessionRepository : ISessionRepository
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<SessionRepository> _logger;
        private readonly AppDbContext _dbContext;
        private const string UserIdKey = "CurrentUserId";
        private const string UsernameKey = "CurrentUsername";

        public SessionRepository(
            IHttpContextAccessor httpContextAccessor,
            ILogger<SessionRepository> logger,
            AppDbContext dbContext)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
            _dbContext = dbContext;
        }

        public async Task<TypingSession> GetByIdAsync(int id)
        {
            try
            {
                var session = await _dbContext.Sessions
                    .Include(s => s.TextSnippet)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (session == null)
                {
                    _logger.LogWarning("Session with ID {SessionId} not found", id);
                    throw new InvalidOperationException($"Session with ID {id} not found");
                }

                return session;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting session with ID {SessionId}", id);
                throw;
            }
        }

        public async Task<IEnumerable<TypingSession>> ListByUserAsync(int userId)
        {
            try
            {
                var sessions = await _dbContext.Sessions
                    .Include(s => s.TextSnippet)
                    .Where(s => s.UserId == userId)
                    .OrderByDescending(s => s.StartTime)
                    .ToListAsync();

                _logger.LogInformation("Retrieved {Count} sessions for user {UserId}", sessions.Count, userId);
                return sessions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing sessions for user {UserId}", userId);
                throw;
            }
        }

        public async Task AddAsync(TypingSession session)
        {
            try
            {
                await _dbContext.Sessions.AddAsync(session);
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Added new session with ID {SessionId} for user {UserId}",
                    session.Id, session.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding session for user {UserId}", session.UserId);
                throw;
            }
        }

        public async Task UpdateAsync(TypingSession session)
        {
            try
            {
                _dbContext.Sessions.Update(session);
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Updated session with ID {SessionId}", session.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating session with ID {SessionId}", session.Id);
                throw;
            }
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
