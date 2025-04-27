using System.Text.Json;
using System.Text.Json.Serialization;

namespace SearchPaperApi.Features.Folders;

public record Folder(
    [property: JsonPropertyName("_id")] string? Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("bucket")] string? Bucket,
    [property: JsonPropertyName("createdAt")] DateTime CreatedAt
);
