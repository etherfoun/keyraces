using Microsoft.AspNetCore.SignalR;
using keyraces.Core.Interfaces;
using keyraces.Core.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.Extensions.Logging;

namespace keyraces.Server.Hubs;

[Authorize(Policy = "SignalRPolicy")]
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
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        var userName = Context.User?.FindFirstValue(ClaimTypes.Name)
                          ?? Context.User?.FindFirstValue("username")
                          ?? Context.User?.FindFirstValue("name")
                          ?? Context.User?.FindFirstValue(ClaimTypes.Email)
                          ?? "Unknown User";

        _logger.LogInformation("Client connected: {ConnectionId}, User: {UserId} ({UserName})",
            Context.ConnectionId, userId, userName);

        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("Unauthenticated user attempted to connect to SignalR hub");
            Context.Abort();
            return;
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception exception)
    {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);

        _logger.LogInformation("Client disconnected: {ConnectionId}, User: {UserId}, Exception: {Exception}",
            Context.ConnectionId, userId, exception?.Message);

        // Cleanup: remove user from all lobbies if they disconnect
        if (!string.IsNullOrEmpty(userId))
        {
            try
            {
                // Note: This assumes your lobby service has a method to remove user from all lobbies
                // If not available, you might need to track which lobby the user was in
                _logger.LogInformation("Cleaning up user {UserId} from lobbies due to disconnection", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up user {UserId} on disconnect", userId);
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinLobby(string lobbyId)
    {
        try
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            var userName = Context.User?.FindFirstValue(ClaimTypes.Name)
                  ?? Context.User?.FindFirstValue("username")
                  ?? Context.User?.FindFirstValue("name")
                  ?? Context.User?.FindFirstValue(ClaimTypes.Email)
                  ?? "Unknown User";

            _logger.LogInformation("User {UserId} ({UserName}) joining lobby {LobbyId}",
                userId, userName, lobbyId);

            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Cannot join lobby: User ID is missing");
                await Clients.Caller.SendAsync("Error", "Authentication required");
                return;
            }

            if (string.IsNullOrEmpty(lobbyId))
            {
                _logger.LogWarning("Cannot join lobby: Lobby ID is missing");
                await Clients.Caller.SendAsync("Error", "Invalid lobby ID");
                return;
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, $"lobby-{lobbyId}");

            await _lobbyService.JoinLobbyAsync(lobbyId, userId, userName);

            await Clients.Group($"lobby-{lobbyId}").SendAsync("UserJoined", userName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in JoinLobby method");
            await Clients.Caller.SendAsync("Error", "Failed to join lobby");
        }
    }

    public async Task LeaveLobby(string lobbyId)
    {
        try
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            var userName = Context.User?.FindFirstValue(ClaimTypes.Name)
                          ?? Context.User?.FindFirstValue("username")
                          ?? Context.User?.FindFirstValue("name")
                          ?? Context.User?.FindFirstValue(ClaimTypes.Email)
                          ?? "Unknown User";

            _logger.LogInformation("User {UserId} ({UserName}) leaving lobby {LobbyId}",
                userId, userName, lobbyId);

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(lobbyId))
            {
                _logger.LogWarning("Cannot leave lobby: User ID or Lobby ID is missing");
                return;
            }

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"lobby-{lobbyId}");

            await Clients.Group($"lobby-{lobbyId}").SendAsync("UserLeft", userName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in LeaveLobby method");
            await Clients.Caller.SendAsync("Error", "Failed to leave lobby");
        }
    }

    public async Task UpdateReadyStatus(string lobbyId, bool isReady)
    {
        try
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            var userName = Context.User?.FindFirstValue(ClaimTypes.Name)
                  ?? Context.User?.FindFirstValue("username")
                  ?? Context.User?.FindFirstValue("name")
                  ?? Context.User?.FindFirstValue(ClaimTypes.Email)
                  ?? "Unknown User";

            _logger.LogInformation("User {UserId} ({UserName}) updating ready status to {IsReady} in lobby {LobbyId}",
                userId, userName, isReady, lobbyId);

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(lobbyId))
            {
                _logger.LogWarning("Cannot update ready status: User ID or Lobby ID is missing");
                return;
            }

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
            await Clients.Caller.SendAsync("Error", "Failed to update ready status");
        }
    }

    public async Task StartGame(string lobbyId, int textSnippetId)
    {
        try
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);

            _logger.LogInformation("User {UserId} starting game in lobby {LobbyId} with text snippet {TextSnippetId}",
                userId, lobbyId, textSnippetId);

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(lobbyId))
            {
                _logger.LogWarning("Cannot start game: User ID or Lobby ID is missing");
                return;
            }

            var lobby = await _lobbyService.GetLobbyAsync(lobbyId);

            if (lobby == null || lobby.HostId != userId)
            {
                _logger.LogWarning("User {UserId} attempted to start game but is not the host of lobby {LobbyId}",
                    userId, lobbyId);
                await Clients.Caller.SendAsync("Error", "Only the host can start the game");
                return;
            }

            await _lobbyService.StartGameAsync(lobbyId, textSnippetId.ToString());

            await Clients.Group($"lobby-{lobbyId}").SendAsync("GameStarted");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in StartGame method");
            await Clients.Caller.SendAsync("Error", "Failed to start game");
        }
    }

    public async Task UpdateProgress(string lobbyId, int progress, int wpm, double accuracy)
    {
        try
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(lobbyId))
            {
                _logger.LogWarning("Cannot update progress: User ID or Lobby ID is missing");
                return;
            }

            await _lobbyService.UpdatePlayerProgressAsync(lobbyId, userId, progress, wpm, accuracy);

            await Clients.Group($"lobby-{lobbyId}").SendAsync("PlayerProgressUpdated", userId, progress, wpm, accuracy);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in UpdateProgress method");
            // Don't send error to caller for progress updates as they happen frequently
        }
    }

    public async Task FinishGame(string lobbyId, int finalWpm, double finalAccuracy)
    {
        try
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);

            _logger.LogInformation("User {UserId} finished game in lobby {LobbyId} with WPM {WPM} and accuracy {Accuracy}",
                userId, lobbyId, finalWpm, finalAccuracy);

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(lobbyId))
            {
                _logger.LogWarning("Cannot finish game: User ID or Lobby ID is missing");
                return;
            }

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
                else
                {
                    await Clients.Caller.SendAsync("Error", "Failed to complete game");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in FinishGame method");
            await Clients.Caller.SendAsync("Error", "Failed to finish game");
        }
    }

    public async Task SendChatMessage(string lobbyId, string message)
    {
        try
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            var userName = Context.User?.FindFirstValue(ClaimTypes.Name)
                          ?? Context.User?.FindFirstValue("username")
                          ?? Context.User?.FindFirstValue("name")
                          ?? Context.User?.FindFirstValue(ClaimTypes.Email)
                          ?? "Unknown User";

            _logger.LogInformation("User {UserId} ({UserName}) sending message in lobby {LobbyId}",
                userId, userName, lobbyId);

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(lobbyId) || string.IsNullOrEmpty(message))
            {
                _logger.LogWarning("Cannot send chat message: Missing required parameters");
                return;
            }

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
            await Clients.Caller.SendAsync("Error", "Failed to send message");
        }
    }

    public async Task SendMessage(string message)
    {
        _logger.LogInformation("Received message: {Message}", message);
        await Clients.Caller.SendAsync("ReceiveMessage", $"Server received: {message}");
    }
}

