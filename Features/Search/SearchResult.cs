using System.Text.Json;
using System.Text.Json.Serialization;
using OpenSearch.Client;

namespace SearchPaperApi.Features.Search;

public record SearchResult(
    [property: JsonPropertyName("_id")] string? Id,
    string UntrustedFileName,
    DateTime UploadDateTime,
    IReadOnlyCollection<string>? Highlight
);
