using Microsoft.Extensions.DependencyInjection;
using SearchPaperApi.Features.Documents;
using SearchPaperApi.Features.Folders;

namespace SearchPaperApi.Extensions;

public static class ServiceCollectionExtencions
{
    public static IServiceCollection AddFeatureServices(this IServiceCollection services)
    {
        services.AddSingleton<DocumentsService>();
        services.AddSingleton<FoldersService>();

        return services;
    }
};
