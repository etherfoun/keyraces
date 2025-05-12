using keyraces.Core.Entities;
using keyraces.Core.Interfaces;
using keyraces.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace keyraces.Infrastructure.Repositories
{
    public class TextSnippetRepository : ITextSnippetRepository
    {
        private readonly AppDbContext _ctx;
        private readonly ILogger<TextSnippetRepository> _logger;

        public TextSnippetRepository(AppDbContext ctx, ILogger<TextSnippetRepository> logger)
        {
            _ctx = ctx;
            _logger = logger;
        }

        public async Task<TextSnippet> GetByIdAsync(int id)
        {
            _logger.LogInformation($"Получение текстового фрагмента по ID: {id}");
            var snippet = await _ctx.TextSnippets.FindAsync(id);
            if (snippet == null)
            {
                _logger.LogWarning($"Текстовый фрагмент с ID {id} не найден");
                throw new InvalidOperationException($"TextSnippet with ID {id} not found.");
            }
            return snippet;
        }

        public async Task<IEnumerable<TextSnippet>> ListAsync()
        {
            _logger.LogInformation("Получение списка текстовых фрагментов");
            return await _ctx.TextSnippets.ToListAsync();
        }

        public async Task<IEnumerable<TextSnippet>> ListAllAsync()
        {
            _logger.LogInformation("Получение полного списка текстовых фрагментов");
            return await _ctx.TextSnippets.ToListAsync();
        }

        public async Task AddAsync(TextSnippet snippet)
        {
            _logger.LogInformation($"Добавление нового текстового фрагмента: {snippet.Content.Substring(0, Math.Min(30, snippet.Content.Length))}...");
            _ctx.TextSnippets.Add(snippet);
            await _ctx.SaveChangesAsync();
            _logger.LogInformation($"Текстовый фрагмент успешно добавлен с ID: {snippet.Id}");
        }

        public async Task UpdateAsync(TextSnippet snippet)
        {
            _logger.LogInformation($"Обновление текстового фрагмента с ID: {snippet.Id}");
            _ctx.TextSnippets.Update(snippet);
            await _ctx.SaveChangesAsync();
            _logger.LogInformation($"Текстовый фрагмент с ID: {snippet.Id} успешно обновлен");
        }

        public async Task DeleteAsync(int id)
        {
            _logger.LogInformation($"Удаление текстового фрагмента с ID: {id}");
            var snippet = await _ctx.TextSnippets.FindAsync(id);
            if (snippet == null)
            {
                _logger.LogWarning($"Текстовый фрагмент с ID {id} не найден для удаления");
                throw new InvalidOperationException($"TextSnippet with ID {id} not found for deletion.");
            }

            _ctx.TextSnippets.Remove(snippet);
            await _ctx.SaveChangesAsync();
            _logger.LogInformation($"Текстовый фрагмент с ID: {id} успешно удален");
        }

        public async Task<bool> ExistsAsync(int id)
        {
            _logger.LogInformation($"Проверка существования текстового фрагмента с ID: {id}");
            return await _ctx.TextSnippets.AnyAsync(t => t.Id == id);
        }

        public async Task<TextSnippet> GetRandomAsync()
        {
            _logger.LogInformation("Получение случайного текстового фрагмента");
            var count = await _ctx.TextSnippets.CountAsync();
            if (count == 0)
            {
                _logger.LogWarning("Нет доступных текстовых фрагментов");
                throw new InvalidOperationException("No text snippets available.");
            }

            var random = new Random();
            var skip = random.Next(count);

            return await _ctx.TextSnippets.Skip(skip).FirstAsync();
        }

        public async Task<TextSnippet> GetRandomByDifficultyAsync(DifficultyLevel difficulty)
        {
            _logger.LogInformation($"Получение случайного текстового фрагмента с уровнем сложности: {difficulty}");
            var snippets = await _ctx.TextSnippets
                .Where(t => t.Difficulty == difficulty)
                .ToListAsync();

            if (!snippets.Any())
            {
                _logger.LogWarning($"Нет доступных текстовых фрагментов с уровнем сложности: {difficulty}");
                throw new InvalidOperationException($"No text snippets available with difficulty level: {difficulty}.");
            }

            var random = new Random();
            var index = random.Next(snippets.Count);

            return snippets[index];
        }
    }
}
