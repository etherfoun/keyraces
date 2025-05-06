namespace keyraces.Server.Dtos
{
    public class StartSessionDto
    {
        public int UserId { get; set; }
        public int TextSnippetId { get; set; }

        public StartSessionDto(int id, int textSnippetId)
        {
            UserId = id;
            TextSnippetId = textSnippetId;
        }
    }
}