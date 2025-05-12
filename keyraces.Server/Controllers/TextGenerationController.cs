using keyraces.Core.Entities;
using keyraces.Core.Interfaces;
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
        [AllowAnonymous]
        public async Task<ActionResult<TextSnippetDto>> GenerateText([FromBody] TextGenerationRequest request)
        {
            try
            {
                _logger.LogInformation($"Generating text with topic: {request.Topic}, difficulty: {request.Difficulty}, length: {request.Length}");

                var generatedText = await _textGenerationService.GenerateTextAsync(
                    request.Topic,
                    request.Difficulty,
                    request.Length);

                if (string.IsNullOrEmpty(generatedText))
                {
                    _logger.LogWarning("Generated text is empty");
                    return BadRequest(new { message = "Не удалось сгенерировать текст: пустой результат" });
                }

                _logger.LogInformation($"Successfully generated text: {generatedText.Substring(0, Math.Min(50, generatedText.Length))}...");

                var textSnippet = new TextSnippet
                {
                    Title = string.IsNullOrEmpty(request.Topic) ? "Сгенерированный текст" : $"Текст на тему: {request.Topic}",
                    Content = generatedText,
                    Difficulty = request.Difficulty,
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
                    Difficulty = savedSnippet.Difficulty
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating text");
                return StatusCode(500, new { message = $"Ошибка при генерации текста: {ex.Message}" });
            }
        }
    }

    public class TextGenerationRequest
    {
        public string Topic { get; set; } = "";
        public string Difficulty { get; set; } = "medium";
        public int Length { get; set; } = 300;
    }
}
