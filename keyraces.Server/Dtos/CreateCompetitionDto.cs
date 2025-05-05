namespace keyraces.Server.Dtos
{
    public class CreateCompetitionDto
    {
        public string Title { get; set; } = string.Empty;
        public int TextSnippetId { get; set; }
        public DateTime StartTime { get; set; }
    }
}