using keyraces.Core.Entities;
using keyraces.Core.Interfaces;
using keyraces.Server.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace keyraces.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TextSnippetController : ControllerBase
    {
        private readonly ITextSnippetRepository _repository;
        private readonly ILogger<TextSnippetController> _logger;

        public TextSnippetController(ITextSnippetRepository repository, ILogger<TextSnippetController> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TextSnippetDto>>> GetAll()
        {
            try
            {
                var snippets = await _repository.GetAllAsync();
                return Ok(snippets.Select(s => new TextSnippetDto
                {
                    Id = s.Id,
                    Title = s.Title,
                    Content = s.Content,
                    Difficulty = s.Difficulty
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all text snippets");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TextSnippetDto>> GetById(int id)
        {
            try
            {
                var snippet = await _repository.GetByIdAsync(id);
                if (snippet == null)
                    return NotFound();

                return Ok(new TextSnippetDto
                {
                    Id = snippet.Id,
                    Title = snippet.Title,
                    Content = snippet.Content,
                    Difficulty = snippet.Difficulty
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting text snippet with id {id}");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet("random")]
        public async Task<ActionResult<TextSnippetDto>> GetRandom([FromQuery] string difficulty = "")
        {
            try
            {
                _logger.LogInformation($"Getting random text snippet with difficulty: {difficulty}");

                var snippet = await _repository.GetRandomAsync(difficulty);

                if (snippet == null && !string.IsNullOrEmpty(difficulty))
                {
                    _logger.LogWarning($"No text snippets found with difficulty: {difficulty}. Trying to get any text snippet.");
                    snippet = await _repository.GetRandomAsync("");
                }

                if (snippet == null)
                {
                    _logger.LogWarning("No text snippets found in the database. Creating a default one.");
                    snippet = new TextSnippet
                    {
                        Title = "Default Text",
                        Content = "This is a default text for typing practice. The quick brown fox jumps over the lazy dog. Pack my box with five dozen liquor jugs. How vexingly quick daft zebras jump!",
                        Difficulty = string.IsNullOrEmpty(difficulty) ? "medium" : difficulty,
                        IsGenerated = false,
                        CreatedAt = DateTime.UtcNow
                    };

                    snippet = await _repository.AddAsync(snippet);
                }

                return Ok(new TextSnippetDto
                {
                    Id = snippet.Id,
                    Title = snippet.Title,
                    Content = snippet.Content,
                    Difficulty = snippet.Difficulty
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting random text snippet");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<ActionResult<TextSnippetDto>> Create([FromBody] CreateTextSnippetDto dto)
        {
            try
            {
                var textSnippet = new TextSnippet
                {
                    Title = dto.Title,
                    Content = dto.Content,
                    Difficulty = dto.Difficulty,
                    IsGenerated = false,
                    CreatedAt = DateTime.UtcNow
                };

                var result = await _repository.AddAsync(textSnippet);

                return CreatedAtAction(nameof(GetById), new { id = result.Id }, new TextSnippetDto
                {
                    Id = result.Id,
                    Title = result.Title,
                    Content = result.Content,
                    Difficulty = result.Difficulty
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating text snippet");
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}
