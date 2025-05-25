namespace keyraces.Server.Dtos
{
    public class GameResultDto
    {
        public string LobbyId { get; set; } = string.Empty;
        public int FinalWPM { get; set; }
        public double FinalAccuracy { get; set; }
    }
}
