using System.IO.Compression;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using OpenSearch.Client;
using SearchPaperApi.Infrastructure;

namespace SearchPaperApi.Features.Documents;

[Route("api/[controller]")]
[ApiController]
public class DocumentsController : ControllerBase
{
    private readonly DocumentsService _documentsService;

    public DocumentsController(DocumentsService documentsService)
    {
        this._documentsService = documentsService;
    }

    [HttpPost("")]
    public async Task<IActionResult> Post(List<IFormFile> files)
    {
        await _documentsService.PutAndIndexAsync(files);
        return Created();
    }

    [HttpGet("")]
    public async Task<IActionResult> GetAll(int size = 7, int page = 0, string term = "")
    {
        var count = await _documentsService.CountAsync(term);

        var pages = Math.Ceiling((double)count / size);

        var offset = page * size;

        var documents = await _documentsService.SearchAsync(size, offset, term);

        Response.Headers.Append("pages", pages.ToString());

        return Ok(documents);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id)
    {
        var document = await _documentsService.GetDocumentAsync(id);

        if (document == null)
        {
            return NotFound();
        }

        var fileName = document.UntrustedFileName;

        HttpContext.Response.Headers.Append("Content-Disposition", $"inline; filename={fileName}");

        var contentType = "application/octet-stream";

        new FileExtensionContentTypeProvider().TryGetContentType(fileName, out contentType);

        var stream = await _documentsService.GetDocumentStreamAsync(document);

        if (stream == null)
        {
            return NotFound();
        }

        return new FileStreamResult(stream, contentType ?? "application/octet-stream");
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var document = await _documentsService.GetDocumentAsync(id);

        if (document == null)
        {
            return NotFound();
        }

        await _documentsService.DeleteDocumentAsync(document);

        return NoContent();
    }

    [HttpGet("zip")]
    public async Task<IActionResult> Zip()
    {
        var stream = await _documentsService.ZipDocumentsAsync();
        return File(stream, "application/octet-stream", S3Storage.DefaultBucket);
    }
}
