using System.IO.Compression;
using System.Web;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using OpenSearch.Client;
using OpenSearch.Net;
using SearchPaperApi.Infrastructure;
using SearchPaperApi.Features.Documents;

namespace SearchPaperApi.Features.Folders;

public class FoldersService
{
    private readonly IOpenSearchClient _openSearchClient;
    private readonly IAmazonS3 _s3Client;

    public FoldersService(IOpenSearchClient openSearchClient, IAmazonS3 s3Client)
    {
        this._openSearchClient = openSearchClient;
        this._s3Client = s3Client;
    }

    public async Task PutAndIndexAsync(Folder folder)
    {
        var bucketName = Guid.NewGuid().ToString();

        await _s3Client.PutBucketAsync(bucketName);

        var indexRequest = new IndexRequest<Folder>(
            new Folder(null, folder.Name, folder.Description, bucketName, DateTime.Now),
            SearchEngine.FoldersIndex
        )
        {
            Refresh = Refresh.True,
        };

        await _openSearchClient.IndexAsync(indexRequest);

        return;
    }

    public async Task<long> CountAsync(string term)
    {
        var countRequest = new CountRequest(SearchEngine.FoldersIndex)
        {
            Query = new BoolQuery
            {
                Should = new List<QueryContainer>
                {
                    new WildcardQuery
                    {
                        Field = "name",
                        Value = $"{term}*",
                        CaseInsensitive = true,
                    },
                    new MatchQuery { Field = "name", Query = term },
                },
            },
        };

        var countResponse = await _openSearchClient.CountAsync(countRequest);

        return countResponse.Count;
    }

    public async Task<Folder?> GetAsync(string id)
    {
        var getRequest = new GetRequest(SearchEngine.FoldersIndex, id);

        var getResponse = await _openSearchClient.GetAsync<Folder>(getRequest);

        if (getResponse.Found == false)
        {
            return null;
        }

        return new Folder(
            getResponse.Id,
            getResponse.Source.Name,
            getResponse.Source.Description,
            getResponse.Source.Bucket,
            getResponse.Source.CreatedAt
        );
    }

    public async Task<List<Folder>> ListAsync(int size, int offset, string term)
    {
        var searchRequest = new SearchRequest(SearchEngine.FoldersIndex)
        {
            Size = size,

            From = offset,

            Query = new BoolQuery
            {
                Should = new List<QueryContainer>
                {
                    new WildcardQuery
                    {
                        Field = "name",
                        Value = $"{term}*",
                        CaseInsensitive = true,
                    },
                    new MatchQuery { Field = "name", Query = term },
                },
            },

            Sort = new List<ISort>
            {
                new FieldSort { Field = "createdAt", Order = SortOrder.Ascending },
            },
        };

        var searchResponse = await _openSearchClient.SearchAsync<Folder>(searchRequest);

        var folders = searchResponse.Hits.Select(h => new Folder(
            h.Id,
            h.Source.Name,
            h.Source.Description,
            h.Source.Bucket,
            h.Source.CreatedAt
        ));

        return folders.ToList();
    }

    public async Task<List<Folder>> ListAsync(IEnumerable<string> ids)
    {
        var queryContainer = new List<QueryContainer> { };

        foreach (string id in ids)
        {
            queryContainer.Add(new TermQuery { Field = "_id", Value = id });
        }

        var searchRequest = new SearchRequest(SearchEngine.FoldersIndex)
        {
            Query = new BoolQuery { Should = queryContainer },
        };

        var searchResponse = await _openSearchClient.SearchAsync<Folder>(searchRequest);

        var folders = searchResponse.Hits.Select(h => new Folder(
            h.Id,
            h.Source.Name,
            h.Source.Description,
            h.Source.Bucket,
            h.Source.CreatedAt
        ));

        return folders.ToList();
    }

    public async Task UpdateAsync(Folder folder)
    {
        var indexRequest = new IndexRequest<Folder>(
            new Folder(folder.Id, folder.Name, folder.Description, folder.Bucket, folder.CreatedAt),
            SearchEngine.FoldersIndex
        )
        {
            Refresh = Refresh.True,
        };

        await _openSearchClient.IndexAsync(indexRequest);
    }

    public async Task DeleteAsync(Folder folder)
    {
        var deleteRequest = new DeleteRequest(SearchEngine.FoldersIndex, folder.Id)
        {
            Refresh = Refresh.True,
        };

        await _openSearchClient.DeleteAsync(deleteRequest);
        await AmazonS3Util.DeleteS3BucketWithObjectsAsync(_s3Client, folder.Bucket);

        return;
    }

    public async Task<Stream> ZipAsync()
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

    public async Task<Stream> ZipAsync(string folderId)
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

            Query = new TermQuery { Field = "folderId", Value = folderId },
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
}
