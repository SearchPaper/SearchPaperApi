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

        double pages = Math.Ceiling((double)count / size);

        var offset = page * size;

        var folders = await _foldersService.ListAsync(size, offset, term);

        Response.Headers.Append("pages", pages.ToString() ?? "0");

        return Ok(folders);
    }

    [HttpGet("count")]
    public async Task<IActionResult> Count(string term = "")
    {
        var count = await _foldersService.CountAsync(term);

        return Ok(count);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Put(string id, Folder folder)
    {
        var formerFolder = await _foldersService.GetAsync(id);

        if (formerFolder == null)
        {
            return NotFound();
        }

        await _foldersService.UpdateAsync(
            new Folder(
                formerFolder.Id,
                folder.Name,
                folder.Description,
                formerFolder.Bucket,
                formerFolder.CreatedAt
            )
        );

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

    [HttpGet("zip")]
    public async Task<IActionResult> Zip()
    {
        var stream = await _foldersService.ZipAsync();
        return File(stream, "application/zip", $"{Path.GetRandomFileName()}.zip");
    }

    [HttpGet("zip/{folderId}")]
    public async Task<IActionResult> Zip(string folderId)
    {
        var stream = await _foldersService.ZipAsync(folderId);
        return File(stream, "application/zip", $"{Path.GetRandomFileName()}.zip");
    }
}
