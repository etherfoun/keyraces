using keyraces.Core.Interfaces;
using keyraces.Core.Models;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace keyraces.Infrastructure.Services
{
    public class RedisCompetitionLobbyService : ICompetitionLobbyService
    {
        private readonly IDistributedCache _cache;
        private const string LOBBY_PREFIX = "lobby:";
        private const string ACTIVE_LOBBIES_KEY = "active_lobbies";
        private const int LOBBY_EXPIRY_HOURS = 2;

        public RedisCompetitionLobbyService(IDistributedCache cache)
        {
            _cache = cache;
        }

        public async Task<CompetitionLobby> CreateLobbyAsync(string hostId, string hostName, string lobbyName, int maxPlayers, bool hasPassword = false, string password = null!)
        {
            var lobby = new CompetitionLobby
            {
                Id = Guid.NewGuid().ToString(),
                Name = lobbyName,
                HostId = hostId,
                HostName = hostName,
                MaxPlayers = maxPlayers,
                Status = LobbyStatus.Waiting,
                CreatedAt = DateTime.UtcNow,
                HasPassword = hasPassword,
                Password = password,
                Players = new List<LobbyPlayer>
                {
                    new LobbyPlayer
                    {
                        UserId = hostId,
                        UserName = hostName,
                        IsHost = true,
                        IsReady = false
                    }
                }
            };

            var key = LOBBY_PREFIX + lobby.Id;
            var json = JsonSerializer.Serialize(lobby);

            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(LOBBY_EXPIRY_HOURS)
            };

            await _cache.SetStringAsync(key, json, options);

            var activeLobbies = await GetActiveLobbiesIdsAsync();
            activeLobbies.Add(lobby.Id);
            await SaveActiveLobbiesIdsAsync(activeLobbies);

            return lobby;
        }

        public async Task<CompetitionLobby> GetLobbyAsync(string lobbyId)
        {
            var key = LOBBY_PREFIX + lobbyId;
            var json = await _cache.GetStringAsync(key);

            if (string.IsNullOrEmpty(json))
                return null!;

            return JsonSerializer.Deserialize<CompetitionLobby>(json);
        }

        public async Task<List<CompetitionLobby>> GetActiveLobbiesAsync()
        {
            var lobbyIds = await GetActiveLobbiesIdsAsync();
            var lobbies = new List<CompetitionLobby>();

            foreach (var lobbyId in lobbyIds)
            {
                var lobby = await GetLobbyAsync(lobbyId);
                if (lobby != null)
                {
                    lobbies.Add(lobby);
                }
            }

            return lobbies;
        }

        public async Task<bool> JoinLobbyAsync(string lobbyId, string userId, string userName, string password = null!)
        {
            var lobby = await GetLobbyAsync(lobbyId);
            if (lobby == null || lobby.Status != LobbyStatus.Waiting)
                return false;

            if (lobby.HasPassword && lobby.Password != password)
                return false;

            if (lobby.Players.Count >= lobby.MaxPlayers)
                return false;

            var existingPlayer = lobby.Players.FirstOrDefault(p => p.UserId == userId);
            if (existingPlayer != null)
                return true;

            lobby.Players.Add(new LobbyPlayer
            {
                UserId = userId,
                UserName = userName,
                IsHost = false,
                IsReady = false
            });

            return await UpdateLobbyAsync(lobby);
        }

        public async Task<bool> LeaveLobbyAsync(string lobbyId, string userId)
        {
            var lobby = await GetLobbyAsync(lobbyId);
            if (lobby == null)
                return false;

            var player = lobby.Players.FirstOrDefault(p => p.UserId == userId);
            if (player == null)
                return false;

            lobby.Players.Remove(player);

            if (player.IsHost)
            {
                if (lobby.Players.Any())
                {
                    var newHost = lobby.Players.First();
                    newHost.IsHost = true;
                    lobby.HostId = newHost.UserId;
                    lobby.HostName = newHost.UserName;
                }
                else
                {
                    return await DeleteLobbyAsync(lobbyId);
                }
            }

            return await UpdateLobbyAsync(lobby);
        }

        public async Task<bool> UpdateReadyStatusAsync(string lobbyId, string userId, bool isReady)
        {
            var lobby = await GetLobbyAsync(lobbyId);
            if (lobby == null || lobby.Status != LobbyStatus.Waiting)
                return false;

            var player = lobby.Players.FirstOrDefault(p => p.UserId == userId);
            if (player == null)
                return false;

            player.IsReady = isReady;
            return await UpdateLobbyAsync(lobby);
        }

        public async Task<bool> StartGameAsync(string lobbyId, string textSnippetId)
        {
            var lobby = await GetLobbyAsync(lobbyId);
            if (lobby == null || lobby.Status != LobbyStatus.Waiting)
                return false;

            lobby.Status = LobbyStatus.InGame;
            lobby.StartedAt = DateTime.UtcNow;
            lobby.TextSnippetId = textSnippetId;

            foreach (var player in lobby.Players)
            {
                player.Progress = 0;
                player.WPM = 0;
                player.Accuracy = 100;
                player.HasFinished = false;
                player.FinalWPM = null;
                player.FinalAccuracy = null;
                player.FinishedAt = null;
                player.Position = null;
            }

            return await UpdateLobbyAsync(lobby);
        }

        public async Task<bool> UpdatePlayerProgressAsync(string lobbyId, string userId, int progress, int wpm, double accuracy)
        {
            var lobby = await GetLobbyAsync(lobbyId);
            if (lobby == null || lobby.Status != LobbyStatus.InGame)
                return false;

            var player = lobby.Players.FirstOrDefault(p => p.UserId == userId);
            if (player == null || player.HasFinished)
                return false;

            player.Progress = progress;
            player.WPM = wpm;
            player.Accuracy = accuracy;

            return await UpdateLobbyAsync(lobby);
        }

        public async Task<bool> PlayerFinishedAsync(string lobbyId, string userId, int finalWpm, double finalAccuracy)
        {
            return await CompleteGameAsync(lobbyId, userId, finalWpm, finalAccuracy);
        }

        public async Task<bool> CompleteGameAsync(string lobbyId, string userId, int finalWpm, double finalAccuracy)
        {
            var lobby = await GetLobbyAsync(lobbyId);
            if (lobby == null || lobby.Status != LobbyStatus.InGame)
                return false;

            var player = lobby.Players.FirstOrDefault(p => p.UserId == userId);
            if (player == null || player.HasFinished)
                return false;

            player.HasFinished = true;
            player.FinalWPM = finalWpm;
            player.FinalAccuracy = finalAccuracy;
            player.FinishedAt = DateTime.UtcNow;
            player.Progress = 100;

            var finishedPlayers = lobby.Players.Where(p => p.HasFinished).OrderBy(p => p.FinishedAt).ToList();
            player.Position = finishedPlayers.Count;

            if (lobby.Players.All(p => p.HasFinished))
            {
                lobby.Status = LobbyStatus.Finished;
            }

            return await UpdateLobbyAsync(lobby);
        }

        public async Task<bool> AddChatMessageAsync(string lobbyId, string userId, string userName, string message)
        {
            var lobby = await GetLobbyAsync(lobbyId);
            if (lobby == null)
                return false;

            var chatMessage = new ChatMessage
            {
                UserId = userId,
                UserName = userName,
                Message = message,
                Timestamp = DateTime.UtcNow
            };

            lobby.ChatMessages.Add(chatMessage);

            if (lobby.ChatMessages.Count > 100)
            {
                lobby.ChatMessages = lobby.ChatMessages.Skip(lobby.ChatMessages.Count - 100).ToList();
            }

            return await UpdateLobbyAsync(lobby);
        }

        public async Task<List<ChatMessage>> GetChatMessagesAsync(string lobbyId)
        {
            var lobby = await GetLobbyAsync(lobbyId);
            return lobby?.ChatMessages ?? new List<ChatMessage>();
        }

        public async Task<bool> DeleteLobbyAsync(string lobbyId)
        {
            var key = LOBBY_PREFIX + lobbyId;
            await _cache.RemoveAsync(key);

            var activeLobbies = await GetActiveLobbiesIdsAsync();
            activeLobbies.Remove(lobbyId);
            await SaveActiveLobbiesIdsAsync(activeLobbies);

            return true;
        }

        private async Task<bool> UpdateLobbyAsync(CompetitionLobby lobby)
        {
            var key = LOBBY_PREFIX + lobby.Id;
            var json = JsonSerializer.Serialize(lobby);

            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(LOBBY_EXPIRY_HOURS)
            };

            await _cache.SetStringAsync(key, json, options);
            return true;
        }

        private async Task<HashSet<string>> GetActiveLobbiesIdsAsync()
        {
            var json = await _cache.GetStringAsync(ACTIVE_LOBBIES_KEY);
            if (string.IsNullOrEmpty(json))
                return new HashSet<string>();

            return JsonSerializer.Deserialize<HashSet<string>>(json);
        }

        private async Task SaveActiveLobbiesIdsAsync(HashSet<string> lobbyIds)
        {
            var json = JsonSerializer.Serialize(lobbyIds);
            await _cache.SetStringAsync(ACTIVE_LOBBIES_KEY, json);
        }
    }
}
