using Amazon.S3;

namespace SearchPaperApi;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // var allowedHosts = builder.Configuration.GetValue<string>("PizzaFlavor");


        // Add services to the container.

        builder.Services.AddControllers();
        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();

        builder.Services.AddSingleton(sp =>
        {
            var serviceURL = builder.Configuration.GetValue<string>("AWS:ServiceURL");
            var accessKey = builder.Configuration.GetValue<string>("AWS:AccessKey");
            var secretKey = builder.Configuration.GetValue<string>("AWS:SecretKey");

            var config = new AmazonS3Config
            {
                ServiceURL = serviceURL,
                RegionEndpoint = Amazon.RegionEndpoint.USEast1,
            };
            var s3Client = new AmazonS3Client(accessKey, secretKey, config);

            return s3Client;
        });

        var app = builder.Build();

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
