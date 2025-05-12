using System.Collections.Generic;
using System.Threading.Tasks;
using keyraces.Core.Entities;

namespace keyraces.Core.Interfaces
{
    public interface ITextSnippetService
    {
        Task<TextSnippet> GetByIdAsync(int id);
        Task<IEnumerable<TextSnippet>> GetAllAsync();
        Task<TextSnippet> GetRandomAsync(string difficulty = "");
        Task<TextSnippet> AddAsync(TextSnippet textSnippet);
        Task UpdateAsync(TextSnippet textSnippet);
        Task DeleteAsync(int id);
    }
}
