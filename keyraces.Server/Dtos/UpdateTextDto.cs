using keyraces.Core.Entities;

namespace keyraces.Server.Dtos
{
    public class UpdateTextDto
    {
        public string Content { get; set; } = string.Empty;
        public DifficultyLevel Difficulty { get; set; }
    }
}