using Azure.Extensions.AspNetCore.DataProtection.Blobs;
using Azure.Storage.Blobs;
using EnhancementHub.Application.Options;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EnhancementHub.Infrastructure.DependencyInjection;

public static class DataProtectionServiceExtensions
{
    public static IServiceCollection ConfigureEnhancementHubDataProtection(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<DataProtectionStorageOptions>(
            configuration.GetSection(DataProtectionStorageOptions.SectionName));

        var options = configuration
            .GetSection(DataProtectionStorageOptions.SectionName)
            .Get<DataProtectionStorageOptions>()
            ?? new DataProtectionStorageOptions();

        var builder = services.AddDataProtection()
            .SetApplicationName(options.ApplicationName);

        var provider = ResolveStorageProvider(options);

        switch (provider)
        {
            case DataProtectionStorageProvider.AzureBlob:
                ConfigureAzureBlob(builder, options);
                break;
            case DataProtectionStorageProvider.FileSystem:
            default:
                ConfigureFileSystem(builder, options);
                break;
        }

        return services;
    }

    public static DataProtectionStorageProvider ResolveStorageProvider(DataProtectionStorageOptions options)
    {
        if (string.Equals(options.StorageProvider, "AzureBlob", StringComparison.OrdinalIgnoreCase))
        {
            return DataProtectionStorageProvider.AzureBlob;
        }

        if (!string.IsNullOrWhiteSpace(options.AzureBlob.ConnectionString)
            && string.Equals(options.StorageProvider, "Auto", StringComparison.OrdinalIgnoreCase))
        {
            return DataProtectionStorageProvider.AzureBlob;
        }

        return DataProtectionStorageProvider.FileSystem;
    }

    private static void ConfigureFileSystem(
        IDataProtectionBuilder builder,
        DataProtectionStorageOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.KeysPath))
        {
            return;
        }

        Directory.CreateDirectory(options.KeysPath);
        builder.PersistKeysToFileSystem(new DirectoryInfo(options.KeysPath));
    }

    private static void ConfigureAzureBlob(
        IDataProtectionBuilder builder,
        DataProtectionStorageOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.AzureBlob.ConnectionString))
        {
            throw new InvalidOperationException(
                "DataProtection:AzureBlob:ConnectionString is required when StorageProvider=AzureBlob.");
        }

        var container = new BlobContainerClient(
            options.AzureBlob.ConnectionString,
            options.AzureBlob.ContainerName);
        container.CreateIfNotExists();

        var blob = container.GetBlobClient(options.AzureBlob.BlobName);
        builder.PersistKeysToAzureBlobStorage(blob);
    }
}

public enum DataProtectionStorageProvider
{
    FileSystem,
    AzureBlob
}
