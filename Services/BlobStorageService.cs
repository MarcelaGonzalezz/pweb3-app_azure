using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace FileShareApp.Services;

public class BlobStorageService
{
    private readonly BlobContainerClient _container;

    public BlobStorageService(IConfiguration config)
    {
        //nombre del servicio de BLobstorage
        var account = config["Storage:AccountName"]
            ?? throw new InvalidOperationException("Falta Storage:AccountName");
        var containerName = config["Storage:ContainerName"] ?? "uploads"; //nombre del contenedor

        // DefaultAzureCredential: Managed Identity en Azure, az login en tu PC
        var runningInAzure = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME"));
        //nosotros elegimos que si esta en azure use ManagedIdentityCredential y si no use AzureCliCredential y VisualStudioCredential
        Azure.Core.TokenCredential credential = runningInAzure
            ? new ManagedIdentityCredential()
            : new ChainedTokenCredential(new AzureCliCredential(), new VisualStudioCredential());

        var service = new BlobServiceClient(
            new Uri($"https://{account}.blob.core.windows.net"),
            credential);

        _container = service.GetBlobContainerClient(containerName);
    }

    public async Task UploadAsync(string blobName, Stream content, string contentType)
    {
        var blob = _container.GetBlobClient(blobName);
        await blob.UploadAsync(content, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
        });
    }

    public async Task<Stream> OpenReadAsync(string blobName)
    {
        var blob = _container.GetBlobClient(blobName);
        return await blob.OpenReadAsync();
    }

    public async Task DeleteAsync(string blobName)
    {
        await _container.GetBlobClient(blobName).DeleteIfExistsAsync();
    }
}