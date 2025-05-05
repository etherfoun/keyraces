using keyraces.Core.Entities;

namespace keyraces.Core.Interfaces
{
    public interface ITextSnippetService
    {
        Task<IEnumerable<TextSnippet>> GetAllAsync();
        Task<TextSnippet> GetByIdAsync(int id);
        Task<TextSnippet> CreateAsync(string content, DifficultyLevel difficulty);
        Task UpdateAsync(int id, string content, DifficultyLevel difficulty);
    }

}
