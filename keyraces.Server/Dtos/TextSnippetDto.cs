namespace keyraces.Server.Dtos
{
    public class TextSnippetDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Difficulty { get; set; } = string.Empty;
        public string Language { get; set; } = "ru";
    }
}
