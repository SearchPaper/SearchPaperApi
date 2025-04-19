using System.IO.Compression;
using Amazon.S3;
using Amazon.S3.Model;
using OpenSearch.Client;
using SearchPaperApi.Infrastructure;

namespace SearchPaperApi.Features.Documents;

public class DocumentsService
{
    private readonly IOpenSearchClient _openSearchClient;
    private readonly IAmazonS3 _s3Client;

    public DocumentsService(IOpenSearchClient openSearchClient, IAmazonS3 s3Client)
    {
        this._openSearchClient = openSearchClient;
        this._s3Client = s3Client;
    }

    public async Task PutAndIndexAsync(List<IFormFile> files)
    {
        for (int i = 0; i < files.Count(); i++)
        {
            var file = files[i];
            var safeDocumentName = Path.GetRandomFileName();

            byte[] bytes;
            using var stream = file.OpenReadStream();
            using var binaryReader = new BinaryReader(stream);

            bytes = binaryReader.ReadBytes((int)stream.Length);

            var contentBase64 = Convert.ToBase64String(bytes);

            var document = new Document(
                null,
                safeDocumentName,
                file.FileName,
                null,
                contentBase64,
                DateTime.Now
            );

            var putObjectRequest = new PutObjectRequest
            {
                BucketName = S3Storage.DefaultBucket,
                Key = safeDocumentName,
                InputStream = stream,
            };
            var indexRequest = new IndexRequest<Document>(document, SearchEngine.DocumentsIndex)
            {
                Pipeline = "AttachmentPipeline",
            };

            await _s3Client.PutObjectAsync(putObjectRequest);
            await _openSearchClient.IndexAsync(indexRequest);
        }
    }

    public async Task<long> CountAsync(string term)
    {
        var countResponse = await _openSearchClient.CountAsync(
            new CountRequest(SearchEngine.DocumentsIndex)
            {
                Query = new WildcardQuery { Field = "untrustedFileName", Value = term },
            }
        );

        return countResponse.Count;
    }

    public async Task<IEnumerable<Document>> SearchAsync(int size, int offset, string term)
    {
        var searchRequest = new SearchRequest(SearchEngine.DocumentsIndex)
        {
            Source = new SourceFilter { Excludes = new Field[] { new Field("attachment") } },
            Size = size,
            From = offset,
            Query = new WildcardQuery { Field = "untrustedFileName", Value = term },
        };

        var searchResponse = await _openSearchClient.SearchAsync<Document>(searchRequest);

        var documents = searchResponse.Hits.Select(d => new Document(
            d.Id,
            d.Source.TrustedFileName,
            d.Source.UntrustedFileName,
            null,
            null,
            d.Source.UploadDateTime
        ));

        return documents;
    }

    public async Task<Document?> GetDocumentAsync(string id)
    {
        var getRequest = new GetRequest<Document>(SearchEngine.DocumentsIndex, id)
        {
            SourceExcludes = new Field[] { new Field("attachment") },
        };

        var response = await _openSearchClient.GetAsync<Document>(getRequest);

        if (!response.Found)
        {
            return null;
        }

        return new Document(
            response.Id,
            response.Source.TrustedFileName,
            response.Source.UntrustedFileName,
            null,
            null,
            response.Source.UploadDateTime
        );
    }

    public async Task<Stream?> GetDocumentStreamAsync(Document document)
    {
        var response = await _s3Client.GetObjectAsync(
            S3Storage.DefaultBucket,
            document.TrustedFileName
        );

        if (response.HttpStatusCode != System.Net.HttpStatusCode.OK)
        {
            return null;
        }

        return response.ResponseStream;
    }

    public async Task DeleteDocumentAsync(Document document)
    {
        var deleteRequest = new DeleteRequest(SearchEngine.DocumentsIndex, document.Id);

        await _openSearchClient.DeleteAsync(deleteRequest);

        await _s3Client.DeleteObjectAsync(S3Storage.DefaultBucket, document.TrustedFileName);

        return;
    }

    public async Task<Stream> ZipDocumentsAsync()
    {
        var searchRequest = new SearchRequest(SearchEngine.DocumentsIndex)
        {
            Source = new SourceFilter
            {
                Includes = new Field[]
                {
                    new Field("trustedFileName"),
                    new Field("untrustedFileName"),
                },
            },
        };

        var searchResponse = await _openSearchClient.SearchAsync<Document>(searchRequest);

        var documents = searchResponse.Documents;

        MemoryStream memoryStream = new MemoryStream();
        using (ZipArchive zipArchive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            foreach (Document document in documents)
            {
                var response = await _s3Client.GetObjectAsync(
                    S3Storage.DefaultBucket,
                    document.TrustedFileName
                );

                if (response.HttpStatusCode != System.Net.HttpStatusCode.OK)
                {
                    continue;
                }

                var zipArchiveEntry = zipArchive.CreateEntry(document.UntrustedFileName);
                using var zipArchiveEntryStream = zipArchiveEntry.Open();
                using var objectStream = response.ResponseStream;
                await objectStream.CopyToAsync(zipArchiveEntryStream);
            }
        }

        memoryStream.Seek(0, SeekOrigin.Begin);

        return memoryStream;
    }
};
