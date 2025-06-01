using keyraces.Core.Entities;
using keyraces.Core.Interfaces;
using keyraces.Core.Dtos;
using keyraces.Infrastructure.Data;
using Microsoft.Extensions.Logging;
namespace keyraces.Infrastructure.Services
{
    public class TypingSessionService : ITypingSessionService
    {
        private readonly ISessionRepository _sessionRepository;
        private readonly ITypingStatisticRepository _statisticRepository;
        private readonly IUserProfileRepository _userProfileRepository;
        private readonly AppDbContext _dbContext;
        private readonly ILogger<TypingSessionService> _logger;

        public TypingSessionService(
            ISessionRepository sessionRepository,
            ITypingStatisticRepository statisticRepository,
            IUserProfileRepository userProfileRepository,
            AppDbContext dbContext,
            ILogger<TypingSessionService> logger)
        {
            _sessionRepository = sessionRepository;
            _statisticRepository = statisticRepository;
            _userProfileRepository = userProfileRepository;
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<TypingSession> StartSessionAsync(int userId, int textSnippetId)
        {
            var session = new TypingSession(userId, textSnippetId);
            await _sessionRepository.AddAsync(session);
            _logger.LogInformation("Started new session {SessionId} for user {UserId} with text snippet {TextSnippetId}", session.Id, userId, textSnippetId);
            return session;
        }

        public async Task CompleteSessionAsync(int sessionId, DateTime endTime)
        {
            var session = await _sessionRepository.GetByIdAsync(sessionId);
            if (session != null)
            {
                session.EndTime = endTime;
                await _sessionRepository.UpdateAsync(session);
                _logger.LogInformation("Completed session {SessionId} at {EndTime}", sessionId, endTime);
            }
            else
            {
                _logger.LogWarning("Attempted to complete non-existent session {SessionId}", sessionId);
            }
        }

        public Task<IEnumerable<TypingSession>> GetByUserAsync(int userId) =>
            _sessionRepository.ListByUserAsync(userId);

        public Task<TypingSession?> GetByIdAsync(int sessionId) =>
            _sessionRepository.GetByIdAsync(sessionId);

        public async Task<TypingSessionResult?> FinalizeAndGetResultAsync(string identityUserId, SessionCompleteRequestDto dto)
        {
            _logger.LogInformation("Attempting to finalize session {SessionId} for identityUser {IdentityUserId}", dto.SessionId, identityUserId);

            var userProfile = await _userProfileRepository.GetByIdentityIdAsync(identityUserId);
            if (userProfile == null)
            {
                _logger.LogWarning("User profile not found for identityUser {IdentityUserId}", identityUserId);
                return null;
            }

            var session = await _sessionRepository.GetByIdAsync(dto.SessionId);
            if (session == null)
            {
                _logger.LogWarning("Session {SessionId} not found for finalization.", dto.SessionId);
                return null;
            }

            if (session.UserId != userProfile.Id)
            {
                _logger.LogWarning("Session {SessionId} (user {SessionUserId}) does not belong to userProfile {UserProfileId} (identityUser {IdentityUserId}). Access denied.",
                    dto.SessionId, session.UserId, userProfile.Id, identityUserId);
                return null;
            }

            if (session.EndTime != default(DateTime))
            {
                _logger.LogWarning("Session {SessionId} is already completed at {EndTime}. Cannot finalize again.", dto.SessionId, session.EndTime);
                return null;
            }

            session.EndTime = session.StartTime.Add(dto.Duration);
            await _sessionRepository.UpdateAsync(session);
            _logger.LogInformation("Session {SessionId} updated and marked as completed with EndTime {EndTime}.", session.Id, session.EndTime);

            var statistic = new TypingStatistic(userProfile.Id, session.Id, dto.Wpm, dto.Accuracy);
            await _statisticRepository.AddAsync(statistic);
            _logger.LogInformation("TypingStatistic created (Id: {StatisticId}) for session {SessionId}. WPM: {WPM}, Accuracy: {Accuracy}.",
                statistic.Id, session.Id, statistic.WPM, statistic.Accuracy);

            var sessionResult = new TypingSessionResult
            {
                TypingSessionId = session.Id,
                UserProfileId = userProfile.Id,
                WPM = dto.Wpm,
                Accuracy = dto.Accuracy,
                ErrorsCount = dto.Errors,
                Duration = dto.Duration,
                CalculatedAtUtc = DateTime.UtcNow
            };

            _dbContext.TypingSessionResults.Add(sessionResult);
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("TypingSessionResult {SessionResultId} created for session {SessionId}.", sessionResult.Id, session.Id);

            return sessionResult;
        }
    }
}
