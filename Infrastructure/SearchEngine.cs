using OpenSearch.Client;

namespace SearchPaperApi.Infrastructure.SearchEngine;

public class SearchEngine
{
    public const string DefaultAttachmentPipeline = "AttachmentPipeline";

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
                    new AttachmentProcessor
                    {
                        Field = new Field("contentBase64"),
                        TargetField = new Field("content"),
                    },
                    new RemoveProcessor { Field = new Field("contentBase64") },
                },
            };

            await searchClient.Ingest.PutPipelineAsync(putPipelineRequest);
        }
    }
}
