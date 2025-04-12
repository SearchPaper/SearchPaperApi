using System.Text.Json;
using System.Text.Json.Serialization;

namespace SearchPaperApi.Features.Documents;

public record Document(
    [property: JsonPropertyName("_id")] string? Id,
    string TrustedFileName,
    string UntrustedFileName,
    Attachment? Attachment,
    string? ContentBase64,
    DateTime UploadDateTime
);

public class Attachment
{
    public string content { get; set; }

    public Attachment(string content)
    {
        this.content = content;
    }
};
