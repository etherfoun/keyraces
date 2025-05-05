using keyraces.Core.Entities;

namespace keyraces.Core.Interfaces
{
    public interface ITextSnippetRepository
    {
        Task<TextSnippet> GetByIdAsync(int id);
        Task<IEnumerable<TextSnippet>> ListAsync();
        Task AddAsync(TextSnippet snippet);
        Task UpdateAsync(TextSnippet snippet);
    }
}
