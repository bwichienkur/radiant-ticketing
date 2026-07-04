using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using EnhancementHub.Application.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EnhancementHub.Infrastructure.Services;

public sealed class S3FileStorageService : IFileStorageService, IDisposable
{
    private readonly IAmazonS3 _s3Client;
    private readonly ILogger<S3FileStorageService> _logger;
    private readonly string _bucket;
    private readonly int _presignedUrlExpiryMinutes;

    public S3FileStorageService(IConfiguration configuration, ILogger<S3FileStorageService> logger)
    {
        _logger = logger;
        _bucket = configuration["Storage:S3:Bucket"]
            ?? throw new InvalidOperationException("Storage:S3:Bucket is required when Storage:Provider=S3.");
        _presignedUrlExpiryMinutes = configuration.GetValue("Storage:S3:PresignedUrlExpiryMinutes", 60);

        var regionName = configuration["Storage:S3:Region"] ?? "us-east-1";
        var region = RegionEndpoint.GetBySystemName(regionName);
        var serviceUrl = configuration["Storage:S3:ServiceUrl"];
        var accessKey = configuration["Storage:S3:AccessKey"];
        var secretKey = configuration["Storage:S3:SecretKey"];

        var config = new AmazonS3Config
        {
            RegionEndpoint = region,
            ForcePathStyle = configuration.GetValue("Storage:S3:ForcePathStyle", false)
        };

        if (!string.IsNullOrWhiteSpace(serviceUrl))
        {
            config.ServiceURL = serviceUrl;
            config.AuthenticationRegion = region.SystemName;
        }

        _s3Client = !string.IsNullOrWhiteSpace(accessKey) && !string.IsNullOrWhiteSpace(secretKey)
            ? new AmazonS3Client(accessKey, secretKey, config)
            : new AmazonS3Client(config);
    }

    public async Task<string> SaveAsync(
        string container,
        string fileName,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var key = BuildKey(container, fileName);
        var request = new PutObjectRequest
        {
            BucketName = _bucket,
            Key = key,
            InputStream = content,
            ContentType = contentType
        };

        await _s3Client.PutObjectAsync(request, cancellationToken);
        _logger.LogDebug("Uploaded object to s3://{Bucket}/{Key}", _bucket, key);
        return key;
    }

    public async Task<Stream> OpenReadAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        var response = await _s3Client.GetObjectAsync(_bucket, storagePath, cancellationToken);
        var memory = new MemoryStream();
        await response.ResponseStream.CopyToAsync(memory, cancellationToken);
        memory.Position = 0;
        return memory;
    }

    public async Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        await _s3Client.DeleteObjectAsync(_bucket, storagePath, cancellationToken);
        _logger.LogDebug("Deleted object s3://{Bucket}/{Key}", _bucket, storagePath);
    }

    public Task<string?> GetPresignedDownloadUrlAsync(
        string storagePath,
        TimeSpan validity,
        CancellationToken cancellationToken = default)
    {
        var expiry = validity > TimeSpan.Zero
            ? validity
            : TimeSpan.FromMinutes(_presignedUrlExpiryMinutes);

        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucket,
            Key = storagePath,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.Add(expiry)
        };

        var url = _s3Client.GetPreSignedURL(request);
        return Task.FromResult<string?>(url);
    }

    internal static string BuildKey(string container, string fileName)
    {
        var safeContainer = SanitizePathSegment(container);
        var safeFileName = SanitizePathSegment(fileName);
        return $"{safeContainer}/{Guid.NewGuid():N}_{safeFileName}";
    }

    private static string SanitizePathSegment(string value) =>
        string.Concat(value.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries))
            .Replace('\\', '/')
            .Trim('/');

    public void Dispose() => _s3Client.Dispose();
}
