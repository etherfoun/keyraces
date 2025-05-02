using keyraces.Core.Entities;
using keyraces.Core.Interfaces;
using keyraces.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace keyraces.Infrastructure.Repositories
{
    public class TextSnippetRepository : ITextSnippetRepository
    {
        private readonly AppDbContext _ctx;
        public TextSnippetRepository(AppDbContext ctx) => _ctx = ctx;

        public async Task<TextSnippet> GetByIdAsync(int id) =>
            await _ctx.TextSnippets.FindAsync(id);

        public async Task<IEnumerable<TextSnippet>> ListAsync() =>
            await _ctx.TextSnippets.ToListAsync();

        public async Task AddAsync(TextSnippet snippet)
        {
            _ctx.TextSnippets.Add(snippet);
            await _ctx.SaveChangesAsync();
        }

        public async Task UpdateAsync(TextSnippet snippet)
        {
            _ctx.TextSnippets.Update(snippet);
            await _ctx.SaveChangesAsync();
        }
    }
}
