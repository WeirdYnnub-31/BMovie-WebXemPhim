using Microsoft.AspNetCore.Mvc;
using webxemphim.Models.DTOs;
using webxemphim.Services;

namespace webxemphim.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class SearchController : ControllerBase
    {
        private readonly AzureSearchService _azureSearch;
        public SearchController(AzureSearchService azureSearch)
        {
            _azureSearch = azureSearch;
        }

        [HttpGet("semantic")] 
        [ProducesResponseType(typeof(PagedResultDto<MovieListItemDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Semantic([FromQuery] string q, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            if (!_azureSearch.IsConfigured) return BadRequest(new { error = "Azure Search not configured" });
            var res = await _azureSearch.SemanticSearchAsync(q, page, pageSize);
            return Ok(res);
        }
    }
}


