using Microsoft.AspNetCore.Mvc;

namespace SearchPaperApi.Features.Folders;

[Route("api/[controller]")]
[ApiController]
public class FoldersController : ControllerBase
{
    private readonly FoldersService _foldersService;

    public FoldersController(FoldersService foldersService)
    {
        this._foldersService = foldersService;
    }

    [HttpPost("")]
    public async Task<IActionResult> Post(Folder folder)
    {
        await _foldersService.PutAndIndexAsync(folder);

        return Created();
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id)
    {
        var folder = await _foldersService.GetAsync(id);

        if (folder == null)
        {
            return NotFound();
        }

        return Ok(folder);
    }

    [HttpGet("")]
    public async Task<IActionResult> GetAll(int size = 7, int page = 0, string term = "")
    {
        var count = await _foldersService.CountAsync(term);

        var pages = Math.Ceiling((double)count / size);

        var offset = page * size;

        var folders = await _foldersService.ListAsync(size, page, term);

        Response.Headers.Append("pages", pages.ToString());

        return Ok(folders);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Put(string id, Folder folder)
    {
        var formerFolder = await _foldersService.GetAsync(id);

        if (formerFolder == null)
        {
            return NotFound();
        }

        await _foldersService.UpdateAsync(folder);

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var folder = await _foldersService.GetAsync(id);

        if (folder == null)
        {
            return NotFound();
        }

        await _foldersService.DeleteAsync(folder);

        return NoContent();
    }
}
