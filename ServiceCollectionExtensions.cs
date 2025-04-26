using Microsoft.Extensions.DependencyInjection;
using SearchPaperApi.Features.Documents;
using SearchPaperApi.Features.Folders;
using SearchPaperApi.Features.Search;

namespace SearchPaperApi.Extensions;

public static class ServiceCollectionExtencions
{
    public static IServiceCollection AddFeatureServices(this IServiceCollection services)
    {
        services.AddSingleton<DocumentsService>();
        services.AddSingleton<FoldersService>();
        services.AddSingleton<SearchService>();

        return services;
    }
};
