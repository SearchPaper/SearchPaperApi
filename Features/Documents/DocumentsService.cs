using Amazon.S3;
using Amazon.S3.Model;
using OpenSearch.Client;
using OpenSearch.Net;
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

    public async Task PutAndIndexAsync(IFormFile file)
    {
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
            DateTime.Now,
            S3Storage.DefaultBucket
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
            Refresh = Refresh.True,
        };

        await _s3Client.PutObjectAsync(putObjectRequest);
        await _openSearchClient.IndexAsync(indexRequest);
    }

    public async Task PutAndIndexAsync(IFormFile file, string folderId)
    {
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
            DateTime.Now,
            folderId
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
            Refresh = Refresh.True,
        };

        await _s3Client.PutObjectAsync(putObjectRequest);
        await _openSearchClient.IndexAsync(indexRequest);
    }

    public async Task<long> CountAsync(string term)
    {
        var countResponse = await _openSearchClient.CountAsync(
            new CountRequest(SearchEngine.DocumentsIndex)
            {
                Query = new BoolQuery
                {
                    Should = new List<QueryContainer>
                    {
                        new WildcardQuery
                        {
                            Field = "untrustedFileName",
                            Value = $"{term}*",
                            CaseInsensitive = true,
                        },
                        new MatchQuery
                        {
                            Field = "untrustedFileName",
                            Query = term,
                            MinimumShouldMatch = 1,
                        },
                    },
                },
            }
        );

        return countResponse.Count;
    }

    public async Task<long> CountAsync(string term, string folderId)
    {
        var countResponse = await _openSearchClient.CountAsync(
            new CountRequest(SearchEngine.DocumentsIndex)
            {
                Query = new BoolQuery
                {
                    Must = new List<QueryContainer>
                    {
                        new TermQuery { Field = "folderId", Value = folderId },
                    },
                    Should = new List<QueryContainer>
                    {
                        new WildcardQuery
                        {
                            Field = "untrustedFileName",
                            Value = $"{term}*",
                            CaseInsensitive = true,
                        },
                        new MatchQuery
                        {
                            Field = "untrustedFileName",
                            Query = term,
                            MinimumShouldMatch = 1,
                        },
                    },
                },
            }
        );

        return countResponse.Count;
    }

    public async Task<IEnumerable<Document>> ListAsync(int size, int offset, string term)
    {
        var searchRequest = new SearchRequest(SearchEngine.DocumentsIndex)
        {
            Source = new SourceFilter { Excludes = new Field[] { new Field("attachment") } },
            Size = size,
            From = offset,
            Query = new BoolQuery
            {
                Should = new List<QueryContainer>
                {
                    new WildcardQuery
                    {
                        Field = "untrustedFileName",
                        Value = $"{term}*",
                        CaseInsensitive = true,
                    },
                    new MatchQuery
                    {
                        Field = "untrustedFileName",
                        Query = term,
                        MinimumShouldMatch = 1,
                    },
                },
            },
            Sort = new List<ISort>
            {
                new FieldSort { Field = "uploadDateTime", Order = SortOrder.Ascending },
            },
        };

        var searchResponse = await _openSearchClient.SearchAsync<Document>(searchRequest);

        var documents = searchResponse.Hits.Select(d => new Document(
            d.Id,
            d.Source.TrustedFileName,
            d.Source.UntrustedFileName,
            null,
            null,
            d.Source.UploadDateTime,
            d.Source.FolderId
        ));

        return documents;
    }

    public async Task<IEnumerable<Document>> ListAsync(
        int size,
        int offset,
        string term,
        string folderId
    )
    {
        var searchRequest = new SearchRequest(SearchEngine.DocumentsIndex)
        {
            Source = new SourceFilter { Excludes = new Field[] { new Field("attachment") } },
            Size = size,
            From = offset,
            Query = new BoolQuery
            {
                Must = new List<QueryContainer>
                {
                    new TermQuery { Field = "folderId", Value = folderId },
                },
                Should = new List<QueryContainer>
                {
                    new WildcardQuery
                    {
                        Field = "untrustedFileName",
                        Value = $"{term}*",
                        CaseInsensitive = true,
                    },
                    new MatchQuery
                    {
                        Field = "untrustedFileName",
                        Query = term,
                        MinimumShouldMatch = 1,
                    },
                },
            },

            Sort = new List<ISort>
            {
                new FieldSort { Field = "uploadDateTime", Order = SortOrder.Ascending },
            },
        };

        var searchResponse = await _openSearchClient.SearchAsync<Document>(searchRequest);

        var documents = searchResponse.Hits.Select(d => new Document(
            d.Id,
            d.Source.TrustedFileName,
            d.Source.UntrustedFileName,
            null,
            null,
            d.Source.UploadDateTime,
            d.Source.FolderId
        ));

        return documents;
    }

    public async Task<Document?> GetAsync(string id)
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
            response.Source.UploadDateTime,
            response.Source.FolderId
        );
    }

    public async Task<Stream?> GetStreamAsync(Document document)
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

    public async Task DeleteAsync(Document document)
    {
        var deleteRequest = new DeleteRequest(SearchEngine.DocumentsIndex, document.Id)
        {
            Refresh = Refresh.True,
        };

        await _openSearchClient.DeleteAsync(deleteRequest);

        await _s3Client.DeleteObjectAsync(S3Storage.DefaultBucket, document.TrustedFileName);

        return;
    }
};
