namespace keyraces.Server.Dtos
{
    public class CreateLobbyDto
    {
        public string Name { get; set; } = string.Empty;
        public int MaxPlayers { get; set; } = 4;
    }
}
