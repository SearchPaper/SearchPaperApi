using System.Text.Json;
using System.Text.Json.Serialization;

namespace SearchPaperApi.Features.Folders;

public record Folder(
    [property: JsonPropertyName("_id")] string? Id,
    string Name,
    string Description,
    string Bucket,
    DateTime CreatedAt
);
