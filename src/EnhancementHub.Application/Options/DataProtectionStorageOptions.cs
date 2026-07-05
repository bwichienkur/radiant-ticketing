namespace EnhancementHub.Application.Options;

public sealed class DataProtectionStorageOptions
{
    public const string SectionName = "DataProtection";

    public string ApplicationName { get; set; } = "EnhancementHub";

    /// <summary>FileSystem (local/NFS mount) or AzureBlob.</summary>
    public string StorageProvider { get; set; } = "FileSystem";

    public string? KeysPath { get; set; }

    public AzureBlobKeyStorageOptions AzureBlob { get; set; } = new();
}

public sealed class AzureBlobKeyStorageOptions
{
    public string? ConnectionString { get; set; }

    public string ContainerName { get; set; } = "dataprotection";

    public string BlobName { get; set; } = "keys.xml";
}
