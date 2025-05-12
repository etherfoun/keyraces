using keyraces.Core.Entities;
using keyraces.Core.Interfaces;

namespace keyraces.Infrastructure
{
    public class TextSnippetService : ITextSnippetService
    {
        private readonly ITextSnippetRepository _repository;

        public TextSnippetService(ITextSnippetRepository repository)
        {
            _repository = repository;
        }

        public async Task<TextSnippet> GetByIdAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task<IEnumerable<TextSnippet>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<TextSnippet> GetRandomAsync(string difficulty = "")
        {
            return await _repository.GetRandomAsync(difficulty);
        }

        public async Task<TextSnippet> AddAsync(TextSnippet textSnippet)
        {
            return await _repository.AddAsync(textSnippet);
        }

        public async Task UpdateAsync(TextSnippet textSnippet)
        {
            await _repository.UpdateAsync(textSnippet);
        }

        public async Task DeleteAsync(int id)
        {
            await _repository.DeleteAsync(id);
        }
    }
}
