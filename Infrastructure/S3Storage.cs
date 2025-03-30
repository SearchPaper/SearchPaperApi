using Amazon.S3;
using Amazon.S3.Util;
using Microsoft.Extensions.DependencyInjection;

namespace SearchPaperApi.Infrastructure.S3Storage;

public class S3Storage
{
    public const string DefaultBucket = "default-bucket";

    public static async Task Initialize(IServiceProvider serviceProvider)
    {
        var s3Client = serviceProvider.GetRequiredService<IAmazonS3>();
        var doesDefaultBucketExist = await AmazonS3Util.DoesS3BucketExistV2Async(
            s3Client,
            DefaultBucket
        );

        if (doesDefaultBucketExist == false)
        {
            await s3Client.PutBucketAsync(DefaultBucket);
        }
    }
}
