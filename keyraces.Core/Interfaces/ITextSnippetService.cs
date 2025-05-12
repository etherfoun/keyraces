using keyraces.Core.Entities;

namespace keyraces.Core.Interfaces
{
    public interface ITextSnippetService
    {
        Task<IEnumerable<TextSnippet>> GetAllAsync();
        Task<TextSnippet> GetByIdAsync(int id);
        Task<TextSnippet> CreateAsync(string content, DifficultyLevel difficulty);
        Task UpdateAsync(int id, string content, DifficultyLevel difficulty);
        Task DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
        Task<TextSnippet> GetRandomAsync();
        Task<TextSnippet> GetRandomByDifficultyAsync(DifficultyLevel difficulty);
        Task SeedTextsFromJsonAsync(string json);
    }
}
