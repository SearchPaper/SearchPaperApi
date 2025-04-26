using System.IO.Compression;
using System.Web;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using OpenSearch.Client;
using SearchPaperApi.Features.Folders;
using SearchPaperApi.Infrastructure;

namespace SearchPaperApi.Features.Documents;

[Route("api/[controller]")]
[ApiController]
public class DocumentsController : ControllerBase
{
    private readonly DocumentsService _documentsService;
    private readonly FoldersService _foldersService;

    public DocumentsController(DocumentsService documentsService, FoldersService foldersService)
    {
        this._documentsService = documentsService;
        this._foldersService = foldersService;
    }

    [HttpPost("")]
    public async Task<IActionResult> Post(List<IFormFile> files)
    {
        await _documentsService.PutAndIndexAsync(files);
        return Created();
    }

    [HttpPost("{folderId}")]
    public async Task<IActionResult> Post(List<IFormFile> files, string folderId)
    {
        await _documentsService.PutAndIndexAsync(files, folderId);
        return Created();
    }

    [HttpGet("")]
    [ActionName("GetAllDocuments")]
    public async Task<IActionResult> GetAll(int size = 7, int page = 0, string term = "")
    {
        var count = await _documentsService.CountAsync(term);

        var pages = Math.Ceiling((double)count / size);

        var offset = page * size;

        var documents = await _documentsService.ListAsync(size, offset, term);

        var folders = await _foldersService.ListAsync(documents.Select(d => d.FolderId));

        Response.Headers.Append("pages", pages.ToString());

        var documentsWithFolders = documents.Select(d =>
        {
            var folder = folders.Find(f => f.Id == d.FolderId);

            return new
            {
                Id = d.Id,
                FileName = d.UntrustedFileName,
                UploadDateTime = d.UploadDateTime,
                Folder = folder,
            };
        });

        return Ok(documentsWithFolders);
    }

    [HttpGet("folder/{folderId}")]
    [ActionName("GetAllDocumentsWithFolder")]
    public async Task<IActionResult> GetAll(
        string folderId,
        int size = 7,
        int page = 0,
        string term = ""
    )
    {
        var count = await _documentsService.CountAsync(term, folderId);

        var pages = Math.Ceiling((double)count / size);

        var offset = page * size;

        var documents = await _documentsService.ListAsync(size, offset, term, folderId);

        var folders = await _foldersService.ListAsync(documents.Select(d => d.FolderId));

        Response.Headers.Append("pages", pages.ToString());

        var documentsWithFolders = documents.Select(d =>
        {
            var folder = folders.Find(f => f.Id == d.FolderId);

            return new
            {
                Id = d.Id,
                FileName = d.TrustedFileName,
                UploadDateTime = d.UploadDateTime,
                Folder = folder,
            };
        });

        return Ok(documentsWithFolders);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id)
    {
        var document = await _documentsService.GetAsync(id);

        if (document == null)
        {
            return NotFound();
        }

        var fileName = document.UntrustedFileName;

        HttpContext.Response.Headers.Append(
            "Content-Disposition",
            $"inline; filename={HttpUtility.UrlEncode(fileName)}"
        );

        var contentType = "application/octet-stream";

        new FileExtensionContentTypeProvider().TryGetContentType(fileName, out contentType);

        var stream = await _documentsService.GetStreamAsync(document);

        if (stream == null)
        {
            return NotFound();
        }

        return new FileStreamResult(stream, contentType ?? "application/octet-stream");
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var document = await _documentsService.GetAsync(id);

        if (document == null)
        {
            return NotFound();
        }

        await _documentsService.DeleteAsync(document);

        return NoContent();
    }

    [HttpGet("zip")]
    public async Task<IActionResult> Zip()
    {
        var stream = await _documentsService.ZipAsync();
        return File(stream, "application/zip", $"{Path.GetRandomFileName()}.zip");
    }

    [HttpGet("zip/{folderId}")]
    public async Task<IActionResult> Zip(string folderId)
    {
        var stream = await _documentsService.ZipAsync(folderId);
        return File(stream, "application/zip", $"{Path.GetRandomFileName()}.zip");
    }
}
