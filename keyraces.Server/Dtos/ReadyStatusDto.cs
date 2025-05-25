namespace keyraces.Server.Dtos
{
    public class ReadyStatusDto
    {
        public string LobbyId { get; set; } = string.Empty;
        public bool IsReady { get; set; }
    }
}
