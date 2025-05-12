using System.Collections.Generic;
using System.Threading.Tasks;
using keyraces.Core.Entities;

namespace keyraces.Core.Interfaces
{
    public interface ITextSnippetRepository
    {
        Task<bool> ExistsAsync(int id);
        Task<TextSnippet> GetByIdAsync(int id);
        Task<IEnumerable<TextSnippet>> GetAllAsync();
        Task<IEnumerable<TextSnippet>> ListAllAsync();
        Task<TextSnippet> GetRandomAsync(string difficulty = "");
        Task<TextSnippet> GetRandomByDifficultyAsync(string difficulty);
        Task<TextSnippet> AddAsync(TextSnippet textSnippet);
        Task UpdateAsync(TextSnippet textSnippet);
        Task DeleteAsync(int id);
    }
}
