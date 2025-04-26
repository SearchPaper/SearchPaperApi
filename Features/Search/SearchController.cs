using Microsoft.AspNetCore.Mvc;
using OpenSearch.Client;

namespace SearchPaperApi.Features.Search;

[Route("api/[controller]")]
[ApiController]
public class SearchController : ControllerBase
{
    private readonly SearchService _searchService;

    public SearchController(SearchService searchService)
    {
        this._searchService = searchService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Search([FromQuery] string q, int size = 7, int page = 0)
    {
        var count = await _searchService.CountAsync(q);

        var offSet = page * size;

        var pages = Math.Ceiling((double)count / size);

        var searchResults = await _searchService.SearchAsync(size, offSet, q);

        Response.Headers.Append("pages", pages.ToString());

        return Ok(searchResults);
    }
}
