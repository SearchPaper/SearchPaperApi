using OpenSearch.Client;

namespace SearchPaperApi.Infrastructure;

public class SearchEngine
{
    public const string DefaultAttachmentPipeline = "AttachmentPipeline";

    public const string DocumentsIndex = "documents";

    public static async Task Initialize(IServiceProvider serviceProvider)
    {
        var searchClient = serviceProvider.GetRequiredService<IOpenSearchClient>();

        var getPipelineRequest = new GetPipelineRequest(DefaultAttachmentPipeline);

        var response = await searchClient.Ingest.GetPipelineAsync(getPipelineRequest);

        if (response.IsValid != true)
        {
            var putPipelineRequest = new PutPipelineRequest(DefaultAttachmentPipeline)
            {
                Description = "The default document ingestion pipeline",
                Processors = new IProcessor[]
                {
                    new AttachmentProcessor { Field = new Field("contentBase64") },
                    new RemoveProcessor { Field = new Field("contentBase64") },
                },
            };

            await searchClient.Ingest.PutPipelineAsync(putPipelineRequest);
        }
    }
}
