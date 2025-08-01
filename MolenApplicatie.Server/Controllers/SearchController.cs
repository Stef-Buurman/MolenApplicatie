using Microsoft.AspNetCore.Mvc;
using MolenApplicatie.Server.Models;
using MolenApplicatie.Server.Services;

namespace EVFraudDetectionSoftware.Server.Controllers
{
    [Route("api/search")]
    [ApiController]
    public class SearchController : ControllerBase
    {
        readonly SearchService _searchService;
        public SearchController(SearchService searchService)
        {
            _searchService = searchService;
        }

        [HttpGet]
        public async Task<ActionResult<SearchResultsModel>> Search([FromQuery] string query, [FromQuery] int limit = 10)
        {
            if (string.IsNullOrWhiteSpace(query))
                return BadRequest("Search query is required");

            if (limit <= 0)
                return BadRequest("Limit must be greater than 0");

            if (limit > 100)
                return BadRequest("Limit cannot exceed 100");

            if (query.Length < 3)
                return BadRequest("Search query must be at least 3 characters long");

            var results = await _searchService.SearchAllAsync(query, limit);
            return Ok(results);
        }
    }
}