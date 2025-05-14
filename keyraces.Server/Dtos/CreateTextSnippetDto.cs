namespace keyraces.Server.Dtos
{
    public class CreateTextSnippetDto
    {
        public string Title { get; set; }
        public string Content { get; set; }
        public string Difficulty { get; set; } = "medium";
        public string Language { get; set; } = "ru";
    }
}
