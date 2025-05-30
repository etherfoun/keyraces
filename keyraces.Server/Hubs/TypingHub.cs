using Microsoft.AspNetCore.SignalR;
using keyraces.Core.Interfaces;
using keyraces.Core.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace keyraces.Server.Hubs
{
    [Authorize]
    public class TypingHub : Hub
    {
        private readonly ICompetitionLobbyService _lobbyService;
        private readonly ILogger<TypingHub> _logger;

        public TypingHub(ICompetitionLobbyService lobbyService, ILogger<TypingHub> logger)
        {
            _lobbyService = lobbyService;
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            _logger.LogInformation("Client disconnected: {ConnectionId}, Exception: {Exception}",
                Context.ConnectionId, exception?.Message);
            await base.OnDisconnectedAsync(exception);
        }

        public async Task JoinLobby(string lobbyId)
        {
            try
            {
                var userId = Context.User.FindFirstValue(ClaimTypes.NameIdentifier);
                var userName = Context.User.FindFirstValue(ClaimTypes.Name)
                      ?? Context.User.FindFirstValue("username")
                      ?? Context.User.FindFirstValue("name")
                      ?? Context.User.FindFirstValue(ClaimTypes.Email)
                      ?? "Unknown User";

                _logger.LogInformation("User {UserId} ({UserName}) joining lobby {LobbyId}",
                    userId, userName, lobbyId);

                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Cannot join lobby: User ID is missing");
                    return;
                }

                await Groups.AddToGroupAsync(Context.ConnectionId, $"lobby-{lobbyId}");

                await _lobbyService.JoinLobbyAsync(lobbyId, userId, userName);

                await Clients.Group($"lobby-{lobbyId}").SendAsync("UserJoined", userName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in JoinLobby method");
                throw;
            }
        }

        public async Task LeaveLobby(string lobbyId)
        {
            try
            {
                var userId = Context.User.FindFirstValue(ClaimTypes.NameIdentifier);
                var userName = Context.User.FindFirstValue(ClaimTypes.Name)
                              ?? Context.User.FindFirstValue("username")
                              ?? Context.User.FindFirstValue("name")
                              ?? Context.User.FindFirstValue(ClaimTypes.Email)
                              ?? "Unknown User";

                _logger.LogInformation("User {UserId} ({UserName}) leaving lobby {LobbyId}",
                    userId, userName, lobbyId);

                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(lobbyId))
                    return;

                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"lobby-{lobbyId}");

                await Clients.Group($"lobby-{lobbyId}").SendAsync("UserLeft", userName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in LeaveLobby method");
                throw;
            }
        }

        public async Task UpdateReadyStatus(string lobbyId, bool isReady)
        {
            try
            {
                var userId = Context.User.FindFirstValue(ClaimTypes.NameIdentifier);
                var userName = Context.User.FindFirstValue(ClaimTypes.Name)
                      ?? Context.User.FindFirstValue("username")
                      ?? Context.User.FindFirstValue("name")
                      ?? Context.User.FindFirstValue(ClaimTypes.Email)
                      ?? "Unknown User";

                _logger.LogInformation("User {UserId} ({UserName}) updating ready status to {IsReady} in lobby {LobbyId}",
                    userId, userName, isReady, lobbyId);

                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(lobbyId))
                    return;

                await _lobbyService.UpdateReadyStatusAsync(lobbyId, userId, isReady);

                var updatedLobby = await _lobbyService.GetLobbyAsync(lobbyId);

                await Clients.Group($"lobby-{lobbyId}").SendAsync("UserReadyStatusChanged", userId, isReady);

                if (updatedLobby != null)
                {
                    await Clients.Group($"lobby-{lobbyId}").SendAsync("ReadyStatusChanged", updatedLobby);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateReadyStatus method");
                throw;
            }
        }

        public async Task StartGame(string lobbyId, int textSnippetId)
        {
            try
            {
                var userId = Context.User.FindFirstValue(ClaimTypes.NameIdentifier);

                _logger.LogInformation("User {UserId} starting game in lobby {LobbyId} with text snippet {TextSnippetId}",
                    userId, lobbyId, textSnippetId);

                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(lobbyId))
                    return;

                var lobby = await _lobbyService.GetLobbyAsync(lobbyId);

                if (lobby == null || lobby.HostId != userId)
                {
                    _logger.LogWarning("User {UserId} attempted to start game but is not the host of lobby {LobbyId}",
                        userId, lobbyId);
                    return;
                }

                await _lobbyService.StartGameAsync(lobbyId, textSnippetId.ToString());

                await Clients.Group($"lobby-{lobbyId}").SendAsync("GameStarted");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in StartGame method");
                throw;
            }
        }

        public async Task UpdateProgress(string lobbyId, int progress, int wpm, double accuracy)
        {
            try
            {
                var userId = Context.User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(lobbyId))
                    return;

                await _lobbyService.UpdatePlayerProgressAsync(lobbyId, userId, progress, wpm, accuracy);

                await Clients.Group($"lobby-{lobbyId}").SendAsync("PlayerProgressUpdated", userId, progress, wpm, accuracy);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateProgress method");
                throw;
            }
        }

        public async Task FinishGame(string lobbyId, int finalWpm, double finalAccuracy)
        {
            try
            {
                var userId = Context.User.FindFirstValue(ClaimTypes.NameIdentifier);

                _logger.LogInformation("User {UserId} finished game in lobby {LobbyId} with WPM {WPM} and accuracy {Accuracy}",
                    userId, lobbyId, finalWpm, finalAccuracy);

                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(lobbyId))
                    return;

                var success = await _lobbyService.CompleteGameAsync(lobbyId, userId, finalWpm, finalAccuracy);

                if (success)
                {
                    var lobby = await _lobbyService.GetLobbyAsync(lobbyId);

                    if (lobby != null)
                    {
                        await Clients.Group($"lobby-{lobbyId}").SendAsync("PlayerFinished", userId, finalWpm, finalAccuracy);

                        await Clients.Group($"lobby-{lobbyId}").SendAsync("LobbyUpdated", lobby);

                        if (lobby.Status == LobbyStatus.Finished)
                        {
                            _logger.LogInformation("All players finished in lobby {LobbyId}", lobbyId);

                            await Clients.Group($"lobby-{lobbyId}").SendAsync("AllPlayersFinished", lobby);

                            var results = lobby.Players
                                .Where(p => p.HasFinished)
                                .OrderBy(p => p.Position)
                                .Select(p => new
                                {
                                    UserId = p.UserId,
                                    UserName = p.UserName,
                                    Position = p.Position,
                                    FinalWPM = p.FinalWPM,
                                    FinalAccuracy = p.FinalAccuracy,
                                    FinishedAt = p.FinishedAt
                                })
                                .ToList();

                            await Clients.Group($"lobby-{lobbyId}").SendAsync("GameResults", results);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in FinishGame method");
                throw;
            }
        }

        public async Task SendChatMessage(string lobbyId, string message)
        {
            try
            {
                var userId = Context.User.FindFirstValue(ClaimTypes.NameIdentifier);
                var userName = Context.User.FindFirstValue(ClaimTypes.Name)
                              ?? Context.User.FindFirstValue("username")
                              ?? Context.User.FindFirstValue("name")
                              ?? Context.User.FindFirstValue(ClaimTypes.Email)
                              ?? "Unknown User";

                _logger.LogInformation("User {UserId} ({UserName}) sending message in lobby {LobbyId}",
                    userId, userName, lobbyId);

                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(lobbyId) || string.IsNullOrEmpty(message))
                    return;

                await _lobbyService.AddChatMessageAsync(lobbyId, userId, userName, message);

                var chatMessage = new ChatMessage
                {
                    UserId = userId,
                    UserName = userName,
                    Message = message,
                    Timestamp = DateTime.UtcNow
                };

                await Clients.Group($"lobby-{lobbyId}").SendAsync("ReceiveChatMessage", chatMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SendChatMessage method");
                throw;
            }
        }

        public async Task SendMessage(string message)
        {
            _logger.LogInformation("Received message: {Message}", message);
            await Clients.Caller.SendAsync("ReceiveMessage", $"Server received: {message}");
        }
    }
}
