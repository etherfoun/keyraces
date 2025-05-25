namespace keyraces.Server.Dtos
{
    public class CreateTextSnippetDto
    {
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Difficulty { get; set; } = "medium";
        public string Language { get; set; } = "en";
    }
}
