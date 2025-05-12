using keyraces.Core.Entities;
using keyraces.Core.Interfaces;
using keyraces.Server.Dtos;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace keyraces.Infrastructure.Services
{
    public class TextSnippetService : ITextSnippetService
    {
        private readonly ITextSnippetRepository _repository;
        private readonly ILogger<TextSnippetService> _logger;

        public TextSnippetService(ITextSnippetRepository repository, ILogger<TextSnippetService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<IEnumerable<TextSnippet>> GetAllAsync()
        {
            return await _repository.ListAllAsync();
        }

        public Task<TextSnippet> GetByIdAsync(int id)
        {
            return _repository.GetByIdAsync(id);
        }

        public async Task<TextSnippet> CreateAsync(string content, DifficultyLevel difficulty)
        {
            var snippet = new TextSnippet(content, difficulty);
            await _repository.AddAsync(snippet);
            return snippet;
        }

        public async Task UpdateAsync(int id, string content, DifficultyLevel difficulty)
        {
            var snippet = await _repository.GetByIdAsync(id);
            snippet.Update(content, difficulty);
            await _repository.UpdateAsync(snippet);
        }

        public async Task DeleteAsync(int id)
        {
            await _repository.DeleteAsync(id);
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _repository.ExistsAsync(id);
        }

        public async Task<TextSnippet> GetRandomAsync()
        {
            return await _repository.GetRandomAsync();
        }

        public async Task<TextSnippet> GetRandomByDifficultyAsync(DifficultyLevel difficulty)
        {
            return await _repository.GetRandomByDifficultyAsync(difficulty);
        }

        public async Task SeedTextsFromJsonAsync(string json)
        {
            _logger.LogInformation("Начало загрузки текстов из JSON");

            try
            {
                var textSnippets = JsonSerializer.Deserialize<List<TextSnippetSeed>>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                _logger.LogInformation($"Десериализовано {textSnippets?.Count ?? 0} записей из JSON");

                if (textSnippets != null && textSnippets.Count > 0)
                {
                    int added = 0;
                    foreach (var seed in textSnippets)
                    {
                        _logger.LogInformation($"Обработка записи: Content='{seed.Content?.Substring(0, Math.Min(20, seed.Content?.Length ?? 0))}...', " +
                                               $"Difficulty={seed.Difficulty}");

                        if (string.IsNullOrWhiteSpace(seed.Content))
                        {
                            _logger.LogWarning("Пропуск записи с пустым содержимым");
                            continue;
                        }

                        if (!Enum.IsDefined(typeof(DifficultyLevel), seed.Difficulty))
                        {
                            _logger.LogWarning($"Пропуск записи с некорректным уровнем сложности: {seed.Difficulty}");
                            continue;
                        }

                        await CreateAsync(seed.Content, (DifficultyLevel)seed.Difficulty);
                        added++;
                    }

                    _logger.LogInformation($"Успешно добавлено {added} текстовых фрагментов в базу данных");
                }
                else
                {
                    _logger.LogWarning("JSON не содержит текстовых фрагментов или имеет неверный формат");
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Ошибка десериализации JSON");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при загрузке текстов из JSON");
                throw;
            }

            _logger.LogInformation("Завершение загрузки текстов из JSON");
        }
    }
}
