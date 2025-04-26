using OpenSearch.Client;
using SearchPaperApi.Features.Documents;
using SearchPaperApi.Features.Folders;
using SearchPaperApi.Infrastructure;

namespace SearchPaperApi.Features.Search;

public class SearchService
{
    private readonly IOpenSearchClient _openSearchClient;

    public SearchService(IOpenSearchClient openSearchClient)
    {
        this._openSearchClient = openSearchClient;
    }

    public async Task<long> CountAsync(string query)
    {
        var countRequest = new CountRequest(SearchEngine.DocumentsIndex)
        {
            Query = new MatchQuery { Field = "attachment.content", Query = query },
        };

        var countResponse = await _openSearchClient.CountAsync(countRequest);

        return countResponse.Count;
    }

    public async Task<IEnumerable<SearchResult>> SearchAsync(int size, int offset, string query)
    {
        var searchRequest = new SearchRequest(SearchEngine.DocumentsIndex)
        {
            Size = size,
            From = offset,
            Source = new SourceFilter { Excludes = new Field[] { new Field("attachment") } },
            Highlight = new Highlight
            {
                Fields = new Dictionary<Field, IHighlightField>
                {
                    {
                        new Field("attachment.content"),
                        new HighlightField()
                        {
                            PreTags = ["<span class='info'>"],
                            PostTags = ["</span>"],
                            ForceSource = true,
                        }
                    },
                },
                Encoder = HighlighterEncoder.Html,
                HighlightQuery = new MatchQuery { Field = "attachment.content", Query = query },
            },
            Query = new MatchQuery { Field = "attachment.content", Query = query },
        };

        var searchResponse = await _openSearchClient.SearchAsync<Document>(searchRequest);
        var searchResults = searchResponse.Hits.Select(h => new SearchResult(
            h.Id,
            h.Source.UntrustedFileName,
            h.Source.UploadDateTime,
            h.Highlight["attachment.content"]
        ));

        return searchResults;
    }
}
