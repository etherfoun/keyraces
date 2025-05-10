using Microsoft.AspNetCore.Mvc;
using keyraces.Core.Entities;
using keyraces.Core.Interfaces;
using keyraces.Server.Dtos;

namespace keyraces.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TextController : ControllerBase
    {
        private readonly ITextSnippetService _service;
        public TextController(ITextSnippetService service) => _service = service;

        [HttpGet]
        public async Task<IEnumerable<TextSnippetDto>> GetAll() => (await _service.GetAllAsync()).
            Select(t => new TextSnippetDto(t.Id, t.Content));

        [HttpGet("{id}")]
        public Task<TextSnippet> Get(int id) => _service.GetByIdAsync(id);

        [HttpPost]
        public async Task<IActionResult> Create(CreateTextDto dto)
        {
            var snippet = await _service.CreateAsync(dto.Content, dto.Difficulty);
            return CreatedAtAction(nameof(Get), new { id = snippet.Id }, snippet);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, UpdateTextDto dto)
        {
            await _service.UpdateAsync(id, dto.Content, dto.Difficulty);
            return NoContent();
        }

        // GET api/text/random?lang=ru&diff=2
        [HttpGet("random")]
        public async Task<IActionResult> GetRandom(
            [FromQuery] string? lang,
            [FromQuery] int? diff)
        {
            var all = await _service.GetAllAsync();
            var filtered = all.Where(t =>
            {
                if (diff.HasValue && t.Difficulty != (DifficultyLevel)diff.Value) return false;
                if (lang != null)
                {
                    bool isCyr = t.Content.Any(c => c >= 'А' && c <= 'я');
                    return lang == "ru" ? isCyr
                         : lang == "en" ? !isCyr
                         : true;
                }
                return true;
            }).ToList();

            if (!filtered.Any()) return NotFound();
            var pick = filtered[new Random().Next(filtered.Count)];
            return Ok(pick);
        }
    }
}
