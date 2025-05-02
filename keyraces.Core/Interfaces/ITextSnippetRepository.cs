using keyraces.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
