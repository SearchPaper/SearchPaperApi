using Microsoft.Extensions.DependencyInjection;
using SearchPaperApi.Features.Documents;

namespace SearchPaperApi.Extensions;

public static class ServiceCollectionExtencions
{
    public static IServiceCollection AddFeatureServices(this IServiceCollection services)
    {
        services.AddSingleton<DocumentsService>();

        return services;
    }
};
