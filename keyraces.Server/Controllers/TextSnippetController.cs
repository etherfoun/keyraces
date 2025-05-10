using keyraces.Core.Interfaces;
using keyraces.Server.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace keyraces.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TextSnippetController : ControllerBase
    {
        private readonly ITextSnippetService _service;
        private readonly Random _rng = new Random();
        public TextSnippetController(ITextSnippetService service)
        {
            _service = service;
        }

        // GET api/textsnippet
        [HttpGet]
        public async Task<IEnumerable<TextSnippetDto>> GetAll()
        {
            var list = await _service.GetAllAsync();
            return list.Select(s => new TextSnippetDto(s.Id, s.Content));
        }

        // GET api/textsnippet/{id}
        [HttpGet("{id:int}")]
        public async Task<ActionResult<TextSnippetDto>> GetById(int id)
        {
            var snippet = await _service.GetByIdAsync(id);
            if (snippet == null) return NotFound();
            return Ok(new TextSnippetDto(snippet.Id, snippet.Content));
        }

        // GET api/textsnippet/random
        [HttpGet("random")]
        public async Task<ActionResult<TextSnippetDto>> GetRandom()
        {
            var all = (await _service.GetAllAsync()).ToList();
            if (!all.Any()) return NotFound();
            var pick = all[_rng.Next(all.Count)];
            return new TextSnippetDto(pick.Id, pick.Content);
        }
    }
}
