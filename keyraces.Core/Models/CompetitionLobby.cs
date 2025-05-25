namespace keyraces.Core.Models
{
    public class CompetitionLobby
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string HostId { get; set; } = string.Empty;
        public string HostName { get; set; } = string.Empty;
        public int MaxPlayers { get; set; }
        public List<LobbyPlayer> Players { get; set; } = new List<LobbyPlayer>();
        public LobbyStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public string TextSnippetId { get; set; } = string.Empty;
        public List<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();
        public bool HasPassword { get; set; }
        public string Password { get; set; } = string.Empty;
    }

    public class LobbyPlayer
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public bool IsReady { get; set; }
        public bool IsHost { get; set; }
        public int Progress { get; set; }
        public int WPM { get; set; }
        public double Accuracy { get; set; }
        public bool HasFinished { get; set; }
        public int? FinalWPM { get; set; }
        public double? FinalAccuracy { get; set; }
        public DateTime? FinishedAt { get; set; }
        public int? Position { get; set; }
    }

    public class ChatMessage
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    public enum LobbyStatus
    {
        Waiting,
        InGame,
        Finished
    }
}
