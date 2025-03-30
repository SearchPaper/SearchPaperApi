using Amazon.S3;
using OpenSearch.Client;
using SearchPaperApi.Infrastructure.S3Storage;
using SearchPaperApi.Infrastructure.SearchEngine;

namespace SearchPaperApi;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // var allowedHosts = builder.Configuration.GetValue<string>("PizzaFlavor");


        // Add services to the container.

        builder.Services.AddControllers();
        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();
        builder.Services.AddSingleton<IAmazonS3>(sp =>
        {
            var serviceURL = builder.Configuration.GetValue<string>("AWS:ServiceURL");
            var accessKey = builder.Configuration.GetValue<string>("AWS:AccessKey");
            var secretKey = builder.Configuration.GetValue<string>("AWS:SecretKey");

            var config = new AmazonS3Config
            {
                ServiceURL = serviceURL,
                AuthenticationRegion = "us-east-1",
                ForcePathStyle = true,
            };
            var s3Client = new AmazonS3Client(accessKey, secretKey, config);

            return s3Client;
        });

        builder.Services.AddSingleton<IOpenSearchClient>(sp =>
        {
            var nodeAddress = builder.Configuration.GetValue<string>("OpenSearch:NodeAddress");

            if (nodeAddress == null)
            {
                throw new NullReferenceException("Search Engine Address is required");
            }

            return new OpenSearchClient(new Uri(nodeAddress));
        });

        var app = builder.Build();

        using (var scope = app.Services.CreateScope())
        {
            var services = scope.ServiceProvider;

            await S3Storage.Initialize(services);
            await SearchEngine.Initialize(services);
        }

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseHttpsRedirection();

        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}
