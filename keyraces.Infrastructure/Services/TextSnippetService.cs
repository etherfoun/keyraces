using keyraces.Core.Entities;
using keyraces.Core.Interfaces;

namespace keyraces.Infrastructure.Services
{
    public class TextSnippetService : ITextSnippetService
    {
        private readonly ITextSnippetRepository _repo;

        public TextSnippetService(ITextSnippetRepository repo)
        {
            _repo = repo;
        }

        public Task<IEnumerable<TextSnippet>> GetAllAsync() => _repo.ListAsync();

        public Task<TextSnippet> GetByIdAsync(int id) => _repo.GetByIdAsync(id);

        public async Task<TextSnippet> CreateAsync(string content, DifficultyLevel difficulty)
        {
            var snippet = new TextSnippet(content, difficulty);
            await _repo.AddAsync(snippet);
            return snippet;
        }

        public async Task UpdateAsync(int id, string content, DifficultyLevel difficulty)
        {
            var snippet = await _repo.GetByIdAsync(id);
            snippet.Update(content, difficulty);
            await _repo.UpdateAsync(snippet);
        }

        public async Task<TextSnippet?> GetRandomAsync()
        {
            var all = await _repo.ListAsync();
            if (!all.Any()) return null;
            var idx = new Random().Next(all.Count());
            return all.ElementAt(idx);
        }
    }
}
