using OpenSearch.Client;
using SearchPaperApi.Features.Documents;
using SearchPaperApi.Features.Folders;

namespace SearchPaperApi.Infrastructure;

public class SearchEngine
{
    public const string DefaultAttachmentPipeline = "AttachmentPipeline";

    public const string DocumentsIndex = "documents";

    public const string FoldersIndex = "folders";

    public static async Task Initialize(IServiceProvider serviceProvider)
    {
        var searchClient = serviceProvider.GetRequiredService<IOpenSearchClient>();

        var getPipelineRequest = new GetPipelineRequest(DefaultAttachmentPipeline);

        var getPipelineResponse = await searchClient.Ingest.GetPipelineAsync(getPipelineRequest);

        if (getPipelineResponse.IsValid != true)
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

        var defaultBucket = S3Storage.DefaultBucket;

        var getRequest = new GetRequest(FoldersIndex, defaultBucket);

        var defaultFolder = await searchClient.GetAsync<Folder>(getRequest);

        if (defaultFolder.Found == false)
        {
            var indexRequest = new IndexRequest<Folder>(FoldersIndex, defaultBucket)
            {
                Document = new Folder(
                    defaultBucket,
                    "Default Folder",
                    "The default folder",
                    defaultBucket,
                    DateTime.Now
                ),
            };

            await searchClient.IndexAsync(indexRequest);
        }

        await searchClient.Indices.CreateAsync(
            DocumentsIndex,
            c =>
                c.Map(m =>
                    m.AutoMap<Document>()
                        .Properties<Document>(p => p.Keyword(k => k.Name(d => d.FolderId)))
                )
        );
    }
}
