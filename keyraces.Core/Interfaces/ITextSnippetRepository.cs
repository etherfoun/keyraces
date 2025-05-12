using keyraces.Core.Entities;

namespace keyraces.Core.Interfaces
{
    public interface ITextSnippetRepository
    {
        Task<TextSnippet> GetByIdAsync(int id);
        Task<IEnumerable<TextSnippet>> ListAsync();
        Task<IEnumerable<TextSnippet>> ListAllAsync();
        Task AddAsync(TextSnippet snippet);
        Task UpdateAsync(TextSnippet snippet);
        Task DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
        Task<TextSnippet> GetRandomAsync();
        Task<TextSnippet> GetRandomByDifficultyAsync(DifficultyLevel difficulty);
    }
}
