using keyraces.Core.Entities;
using keyraces.Core.Interfaces;
using keyraces.Infrastructure.Services;
using keyraces.Server.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace keyraces.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TextGenerationController : ControllerBase
    {
        private readonly ITextGenerationService _textGenerationService;
        private readonly ITextSnippetRepository _textSnippetRepository;
        private readonly ILogger<TextGenerationController> _logger;

        public TextGenerationController(
            ITextGenerationService textGenerationService,
            ITextSnippetRepository textSnippetRepository,
            ILogger<TextGenerationController> logger)
        {
            _textGenerationService = textGenerationService;
            _textSnippetRepository = textSnippetRepository;
            _logger = logger;
        }

        [HttpPost("generate")]
        [AllowAnonymous] // Оставляем AllowAnonymous, так как есть проблемы с авторизацией
        public async Task<ActionResult<TextSnippetDto>> GenerateText([FromBody] TextGenerationRequest request)
        {
            try
            {
                _logger.LogInformation($"Generating text with topic: {request.Topic}, difficulty: {request.Difficulty}, length: {request.Length}, language: {request.Language}, timestamp: {request.Timestamp}");

                if (request.Length < 50 || request.Length > 1000)
                {
                    return BadRequest(new { message = "Длина текста должна быть от 50 до 1000 символов" });
                }

                if (string.IsNullOrEmpty(request.Language) || (request.Language != "ru" && request.Language != "en"))
                {
                    request.Language = "ru";
                }

                if (string.IsNullOrEmpty(request.Difficulty) ||
                    (request.Difficulty != "easy" && request.Difficulty != "medium" && request.Difficulty != "hard"))
                {
                    request.Difficulty = "medium";
                }

                var generatedText = await _textGenerationService.GenerateTextAsync(
                    request.Topic,
                    request.Difficulty,
                    request.Length,
                    request.Language);

                if (string.IsNullOrEmpty(generatedText))
                {
                    _logger.LogWarning("Generated text is empty");
                    return BadRequest(new { message = "Не удалось сгенерировать текст: пустой результат" });
                }

                _logger.LogInformation($"Successfully generated text: {generatedText.Substring(0, Math.Min(50, generatedText.Length))}...");

                var textSnippet = new TextSnippet
                {
                    Title = string.IsNullOrEmpty(request.Topic)
                        ? (request.Language.ToLower() == "en" ? "Generated Text" : "Сгенерированный текст")
                        : (request.Language.ToLower() == "en" ? $"Text about: {request.Topic}" : $"Текст на тему: {request.Topic}"),
                    Content = generatedText,
                    Difficulty = request.Difficulty,
                    Language = request.Language,
                    IsGenerated = true,
                    CreatedAt = DateTime.UtcNow
                };

                _logger.LogInformation("Saving generated text to database");
                var savedSnippet = await _textSnippetRepository.AddAsync(textSnippet);
                _logger.LogInformation($"Text saved with ID: {savedSnippet.Id}");

                return Ok(new TextSnippetDto
                {
                    Id = savedSnippet.Id,
                    Title = savedSnippet.Title,
                    Content = savedSnippet.Content,
                    Difficulty = savedSnippet.Difficulty,
                    Language = savedSnippet.Language
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating text");
                return StatusCode(500, new { message = $"Ошибка при генерации текста: {ex.Message}" });
            }
        }

        [HttpPost("reset-context")]
        [AllowAnonymous]
        public async Task<ActionResult> ResetOllamaContext()
        {
            try
            {
                if (_textGenerationService is LocalLLMTextGenerationService localService)
                {
                    var methodInfo = localService.GetType().GetMethod("ResetOllamaContext",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                    if (methodInfo != null)
                    {
                        await (Task)methodInfo.Invoke(localService, null);
                        return Ok(new { message = "Контекст Ollama успешно сброшен" });
                    }
                    else
                    {
                        return StatusCode(500, new { message = "Метод ResetOllamaContext не найден" });
                    }
                }

                return Ok(new { message = "Сервис не поддерживает сброс контекста" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting Ollama context");
                return StatusCode(500, new { message = $"Ошибка при сбросе контекста Ollama: {ex.Message}" });
            }
        }
    }

    public class TextGenerationRequest
    {
        public string Topic { get; set; } = "";
        public string Difficulty { get; set; } = "medium";
        public int Length { get; set; } = 300;
        public string Language { get; set; } = "ru"; 
        public long Timestamp { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }
}
