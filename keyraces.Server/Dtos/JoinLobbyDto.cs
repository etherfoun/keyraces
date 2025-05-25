namespace keyraces.Server.Dtos
{
    public class JoinLobbyDto
    {
        public string LobbyId { get; set; } = string.Empty;
        public string? Password { get; set; } = null;
    }
}
