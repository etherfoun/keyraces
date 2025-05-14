using keyraces.Core.Entities;
using keyraces.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using keyraces.Infrastructure.Data;
using Microsoft.Extensions.Logging;

namespace keyraces.Infrastructure
{
    public class TextSnippetRepository : ITextSnippetRepository
    {
        private readonly AppDbContext _context;
        private readonly Random _random = new Random();
        private readonly ILogger<TextSnippetRepository> _logger;

        public TextSnippetRepository(AppDbContext context, ILogger<TextSnippetRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.TextSnippets.AnyAsync(t => t.Id == id);
        }

        public async Task<TextSnippet> GetByIdAsync(int id)
        {
            return await _context.TextSnippets.FindAsync(id);
        }

        public async Task<IEnumerable<TextSnippet>> GetAllAsync()
        {
            return await _context.TextSnippets.ToListAsync();
        }

        public async Task<IEnumerable<TextSnippet>> ListAllAsync()
        {
            return await _context.TextSnippets.ToListAsync();
        }

        public async Task<TextSnippet> GetRandomAsync(string difficulty = "", string language = "ru")
        {
            try
            {
                var query = _context.TextSnippets.AsQueryable();

                if (!string.IsNullOrEmpty(difficulty))
                {
                    _logger.LogInformation($"Filtering by difficulty: {difficulty}");
                    query = query.Where(t => t.Difficulty == difficulty);
                }

                _logger.LogInformation($"Filtering by language: {language}");
                query = query.Where(t => t.Language == language);

                var count = await query.CountAsync();
                _logger.LogInformation($"Found {count} text snippets matching criteria");

                if (count == 0)
                {
                    _logger.LogWarning("No text snippets found with the specified criteria");
                    return null;
                }

                var randomIndex = _random.Next(0, count);
                _logger.LogInformation($"Selected random index: {randomIndex}");

                return await query.Skip(randomIndex).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting random text snippet");
                throw;
            }
        }

        public async Task<TextSnippet> GetRandomByDifficultyAsync(string difficulty, string language = "ru")
        {
            return await GetRandomAsync(difficulty, language);
        }

        public async Task<TextSnippet> AddAsync(TextSnippet textSnippet)
        {
            try
            {
                _logger.LogInformation($"Adding new text snippet: {textSnippet.Title}");
                await _context.TextSnippets.AddAsync(textSnippet);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Text snippet added with ID: {textSnippet.Id}");
                return textSnippet;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding text snippet");
                throw;
            }
        }

        public async Task UpdateAsync(TextSnippet textSnippet)
        {
            try
            {
                _logger.LogInformation($"Updating text snippet with ID: {textSnippet.Id}");
                _context.TextSnippets.Update(textSnippet);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Text snippet updated: {textSnippet.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating text snippet with ID: {textSnippet.Id}");
                throw;
            }
        }

        public async Task DeleteAsync(int id)
        {
            try
            {
                _logger.LogInformation($"Deleting text snippet with ID: {id}");
                var textSnippet = await _context.TextSnippets.FindAsync(id);
                if (textSnippet != null)
                {
                    _context.TextSnippets.Remove(textSnippet);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Text snippet deleted: {id}");
                }
                else
                {
                    _logger.LogWarning($"Text snippet with ID {id} not found for deletion");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting text snippet with ID: {id}");
                throw;
            }
        }

        public async Task ClearAllAsync()
        {
            try
            {
                _logger.LogWarning("Clearing all text snippets from database");

                await _context.Database.ExecuteSqlRawAsync("DELETE FROM \"TextSnippets\"");

                _logger.LogWarning("All text snippets have been cleared");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing text snippets");
                throw;
            }
        }
    }
}
