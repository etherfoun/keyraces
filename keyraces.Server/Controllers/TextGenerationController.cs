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
        private readonly IHttpClientFactory _httpClientFactory;

        public TextGenerationController(
            ITextGenerationService textGenerationService,
            ITextSnippetRepository textSnippetRepository,
            IHttpClientFactory httpClientFactory,
            ILogger<TextGenerationController> logger)
        {
            _textGenerationService = textGenerationService;
            _textSnippetRepository = textSnippetRepository;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        [HttpPost("generate")]
        [AllowAnonymous]
        public async Task<ActionResult<TextSnippetDto>> GenerateText([FromBody] TextGenerationRequest request)
        {
            try
            {
                _logger.LogInformation($"Generating text with topic: {request.Topic}, difficulty: {request.Difficulty}, length: {request.Length}, language: {request.Language}, timestamp: {request.Timestamp}");

                if (request.Length < 50 || request.Length > 1000)
                {
                    return BadRequest(new { message = "Text length must be between 50 and 1000 characters" });
                }

                if (string.IsNullOrEmpty(request.Language) || (request.Language != "ru" && request.Language != "en" && request.Language != "uk"))
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
                    return BadRequest(new { message = "Failed to generate text: empty result" });
                }

                _logger.LogInformation($"Successfully generated text: {generatedText.Substring(0, Math.Min(50, generatedText.Length))}...");

                string title;
                if (string.IsNullOrEmpty(request.Topic))
                {
                    switch (request.Language.ToLower())
                    {
                        case "en":
                            title = "Generated Text";
                            break;
                        case "uk":
                            title = "Generated text";
                            break;
                        default:
                            title = "Generated Text"; 
                            break;
                    }
                }
                else
                {
                    switch (request.Language.ToLower())
                    {
                        case "en":
                            title = $"Text about: {request.Topic}";
                            break;
                        case "uk":
                            title = $"Text on the topic: {request.Topic}";
                            break;
                        default:
                            title = $"Text on the topic: {request.Topic}"; 
                            break;
                    }
                }

                var textSnippet = new TextSnippet
                {
                    Title = title,
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
                return StatusCode(500, new { message = $"Error generating text: {ex.Message}" });
            }
        }

        [HttpGet("check-connection")]
        [AllowAnonymous]
        public async Task<ActionResult> CheckOllamaConnection()
        {
            try
            {
                _logger.LogInformation("Checking Ollama connection");

                string ollamaUrl = "";
                if (_textGenerationService is LocalLLMTextGenerationService localService)
                {
                    var fieldInfo = localService.GetType().GetField("_apiUrl",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                    if (fieldInfo != null)
                    {
                        ollamaUrl = (string)fieldInfo.GetValue(localService);
                    }
                }

                if (string.IsNullOrEmpty(ollamaUrl))
                {
                    ollamaUrl = "http://localhost:11434/api/generate";
                }

                var healthUrl = ollamaUrl.Replace("/api/generate", "/");
                _logger.LogInformation($"Testing Ollama connection at: {healthUrl}");

                using var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(5);

                var response = await client.GetAsync(healthUrl);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();

                    var modelUrl = healthUrl + "api/tags";
                    var modelResponse = await client.GetAsync(modelUrl);
                    var modelContent = await modelResponse.Content.ReadAsStringAsync();

                    return Ok(new
                    {
                        status = "success",
                        message = "Ollama connection successful",
                        url = healthUrl,
                        response = content,
                        models = modelContent
                    });
                }
                else
                {
                    return StatusCode((int)response.StatusCode, new
                    {
                        status = "error",
                        message = $"Ollama returned status code: {response.StatusCode}",
                        url = healthUrl
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking Ollama connection");
                return StatusCode(500, new
                {
                    status = "error",
                    message = $"Error checking Ollama connection: {ex.Message}",
                    exceptionType = ex.GetType().Name
                });
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
                        return Ok(new { message = "Ollama context successfully reset" });
                    }
                    else
                    {
                        return StatusCode(500, new { message = "ResetOllamaContext method not found" });
                    }
                }

                return Ok(new { message = "Service does not support context reset" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting Ollama context");
                return StatusCode(500, new { message = $"Error resetting Ollama context: {ex.Message}" });
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