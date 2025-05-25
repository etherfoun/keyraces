using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using keyraces.Core.Models;

namespace keyraces.Core.Interfaces
{
    public interface ICompetitionLobbyService
    {
        Task<CompetitionLobby> CreateLobbyAsync(string hostId, string hostName, string lobbyName, int maxPlayers, bool hasPassword = false, string password = null);
        Task<CompetitionLobby> GetLobbyAsync(string lobbyId);
        Task<bool> JoinLobbyAsync(string lobbyId, string userId, string userName, string password = null);
        Task<bool> LeaveLobbyAsync(string lobbyId, string userId);
        Task<bool> UpdateReadyStatusAsync(string lobbyId, string userId, bool isReady);
        Task<bool> StartGameAsync(string lobbyId, string textSnippetId);
        Task<bool> AddChatMessageAsync(string lobbyId, string userId, string userName, string message);
        Task<bool> UpdatePlayerProgressAsync(string lobbyId, string userId, int progress, int wpm, double accuracy);
        Task<bool> PlayerFinishedAsync(string lobbyId, string userId, int finalWpm, double finalAccuracy);
        Task<bool> CompleteGameAsync(string lobbyId, string userId, int finalWpm, double finalAccuracy);
        Task<List<CompetitionLobby>> GetActiveLobbiesAsync();
        Task<bool> DeleteLobbyAsync(string lobbyId);
        Task<List<ChatMessage>> GetChatMessagesAsync(string lobbyId);
    }
}
