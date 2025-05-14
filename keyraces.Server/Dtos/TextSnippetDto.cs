namespace keyraces.Server.Dtos
{
    public class TextSnippetDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string Difficulty { get; set; }
        public string Language { get; set; } = "ru";
    }
}
