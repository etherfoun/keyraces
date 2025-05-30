using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using keyraces.Core.Interfaces;
using keyraces.Server.Dtos;
using keyraces.Server.Hubs;
using System.Security.Claims;

namespace keyraces.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class LobbyController : ControllerBase
    {
        private readonly ICompetitionLobbyService _lobbyService;
        private readonly IHubContext<TypingHub> _hubContext;

        public LobbyController(ICompetitionLobbyService lobbyService, IHubContext<TypingHub> hubContext)
        {
            _lobbyService = lobbyService;
            _hubContext = hubContext;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateLobby([FromBody] CreateLobbyDto dto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userName = User.FindFirst(ClaimTypes.Name)?.Value
                          ?? User.FindFirst("unique_name")?.Value
                          ?? User.FindFirst("name")?.Value
                          ?? User.Identity?.Name;

            Console.WriteLine($"Creating lobby - UserId: {userId}, UserName: {userName}");
            Console.WriteLine($"Available claims: {string.Join(", ", User.Claims.Select(c => $"{c.Type}={c.Value}"))}");

            if (string.IsNullOrEmpty(userId))
            {
                Console.WriteLine("User ID is null or empty");
                return BadRequest("User ID not found.");
            }

            if (string.IsNullOrEmpty(userName))
            {
                Console.WriteLine("User name is null or empty, using fallback");
                userName = $"User_{userId.Substring(0, 8)}";
            }

            try
            {
                var lobby = await _lobbyService.CreateLobbyAsync(userId, userName, dto.Name, dto.MaxPlayers);
                Console.WriteLine($"Lobby created successfully: {lobby.Id} with host {lobby.HostName} ({lobby.HostId})");

                await _hubContext.Clients.All.SendAsync("LobbyCreated", lobby);

                return Ok(lobby);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating lobby: {ex.Message}");
                return StatusCode(500, "Failed to create lobby");
            }
        }

        [HttpGet("active")]
        public async Task<IActionResult> GetActiveLobbies()
        {
            var lobbies = await _lobbyService.GetActiveLobbiesAsync();
            return Ok(lobbies);
        }

        [HttpGet("{lobbyId}")]
        public async Task<IActionResult> GetLobby(string lobbyId)
        {
            var lobby = await _lobbyService.GetLobbyAsync(lobbyId);
            if (lobby == null)
                return NotFound();

            return Ok(lobby);
        }

        [HttpPost("join")]
        public async Task<IActionResult> JoinLobby([FromBody] JoinLobbyDto dto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? User.Identity?.Name;

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(userName))
            {
                return BadRequest("User ID or User Name is missing.");
            }

            var success = await _lobbyService.JoinLobbyAsync(dto.LobbyId, userId, userName);
            if (!success)
                return BadRequest("Unable to join lobby");

            var lobby = await _lobbyService.GetLobbyAsync(dto.LobbyId);

            await _hubContext.Clients.Group($"lobby-{dto.LobbyId}").SendAsync("PlayerJoined", lobby);

            return Ok(lobby);
        }

        [HttpPost("leave/{lobbyId}")]
        public async Task<IActionResult> LeaveLobby(string lobbyId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest("User ID is missing.");
            }

            var success = await _lobbyService.LeaveLobbyAsync(lobbyId, userId);
            if (!success)
                return BadRequest("Unable to leave lobby");

            var lobby = await _lobbyService.GetLobbyAsync(lobbyId);

            await _hubContext.Clients.Group($"lobby-{lobbyId}").SendAsync("PlayerLeft", lobby);

            return Ok();
        }

        [HttpPost("ready")]
        public async Task<IActionResult> UpdateReadyStatus([FromBody] ReadyStatusDto dto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest("User ID is missing.");
            }

            var success = await _lobbyService.UpdateReadyStatusAsync(dto.LobbyId, userId, dto.IsReady);
            if (!success)
                return BadRequest("Unable to update ready status");

            var lobby = await _lobbyService.GetLobbyAsync(dto.LobbyId);

            await _hubContext.Clients.Group($"lobby-{dto.LobbyId}").SendAsync("ReadyStatusChanged", lobby);

            return Ok();
        }

        [HttpPost("start")]
        public async Task<IActionResult> StartGame([FromBody] StartGameDto dto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var lobby = await _lobbyService.GetLobbyAsync(dto.LobbyId);

            if (lobby == null || lobby.HostId != userId)
                return Unauthorized("Only the host can start the game");

            var success = await _lobbyService.StartGameAsync(dto.LobbyId, dto.TextSnippetId);
            if (!success)
                return BadRequest("Unable to start game");

            lobby = await _lobbyService.GetLobbyAsync(dto.LobbyId);

            await _hubContext.Clients.Group($"lobby-{dto.LobbyId}").SendAsync("GameStarted", lobby);

            return Ok();
        }

        [HttpPost("complete")]
        public async Task<IActionResult> CompleteGame([FromBody] GameResultDto dto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest("User ID is missing.");
            }

            var success = await _lobbyService.CompleteGameAsync(dto.LobbyId, userId, dto.FinalWPM, dto.FinalAccuracy);
            if (!success)
                return BadRequest("Unable to complete game");

            var lobby = await _lobbyService.GetLobbyAsync(dto.LobbyId);

            await _hubContext.Clients.Group($"lobby-{dto.LobbyId}").SendAsync("PlayerFinished", lobby);

            return Ok();
        }

        [HttpPost("chat")]
        public async Task<IActionResult> SendChatMessage([FromBody] ChatMessageDto dto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? User.Identity?.Name;

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(userName))
            {
                return BadRequest("User ID or User Name is missing.");
            }

            var success = await _lobbyService.AddChatMessageAsync(dto.LobbyId, userId, userName, dto.Message);
            if (!success)
                return BadRequest("Unable to send message");

            await _hubContext.Clients.Group($"lobby-{dto.LobbyId}").SendAsync("ChatMessage", new
            {
                userId,
                userName,
                message = dto.Message,
                timestamp = DateTime.UtcNow
            });

            return Ok();
        }

        [HttpGet("{lobbyId}/chat")]
        public async Task<IActionResult> GetChatMessages(string lobbyId)
        {
            var messages = await _lobbyService.GetChatMessagesAsync(lobbyId);
            return Ok(messages);
        }

        [HttpDelete("{lobbyId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteLobby(string lobbyId)
        {
            try
            {
                var lobby = await _lobbyService.GetLobbyAsync(lobbyId);
                if (lobby == null)
                {
                    return NotFound(new { message = "Lobby not found" });
                }

                var success = await _lobbyService.DeleteLobbyAsync(lobbyId);
                if (success)
                {
                    await _hubContext.Clients.All.SendAsync("LobbyDeleted", lobbyId);
                    return Ok(new { message = "Lobby deleted successfully" });
                }
                else
                {
                    return BadRequest(new { message = "Cannot delete a lobby" });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while deleting a lobby: {ex.Message}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpDelete("force/{lobbyId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ForceDeleteLobby(string lobbyId)
        {
            try
            {
                var success = await _lobbyService.DeleteLobbyAsync(lobbyId);
                if (success)
                {
                    await _hubContext.Clients.All.SendAsync("LobbyDeleted", lobbyId);
                    return Ok(new { message = "Lobby deleted successfully" });
                }
                else
                {
                    return BadRequest(new { message = "Cannot delete a lobby" });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while deleting a lobby: {ex.Message}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
    }
}
