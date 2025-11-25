using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;

namespace webxemphim.Services
{
    public class BlobStorageService
    {
        private readonly string? _conn;
        private readonly string _container;
        private readonly BlobContainerClient? _containerClient;
        private readonly IWebHostEnvironment _env;

        public bool IsConfigured => _containerClient != null;

        public BlobStorageService(IConfiguration configuration, IWebHostEnvironment env)
        {
            _env = env;
            _conn = configuration["BlobStorage:ConnectionString"];
            _container = configuration["BlobStorage:Container"] ?? "uploads";
            if (!string.IsNullOrWhiteSpace(_conn))
            {
                var svc = new BlobServiceClient(_conn);
                _containerClient = svc.GetBlobContainerClient(_container);
                try { _containerClient.CreateIfNotExists(PublicAccessType.None); } catch { }
            }
        }

        public async Task<string> UploadAsync(Stream stream, string contentType, string fileName)
        {
            if (_containerClient == null)
            {
                // Fallback save to wwwroot/uploads
                var dir = Path.Combine(_env.WebRootPath, "uploads", "images");
                Directory.CreateDirectory(dir);
                var extension = Path.GetExtension(fileName);
                if (string.IsNullOrWhiteSpace(extension))
                {
                    // Determine extension from content type
                    extension = contentType?.ToLowerInvariant() switch
                    {
                        "image/jpeg" or "image/jpg" => ".jpg",
                        "image/png" => ".png",
                        "image/webp" => ".webp",
                        _ => ".jpg"
                    };
                }
                var name = $"{Guid.NewGuid():N}{extension}";
                var path = Path.Combine(dir, name);
                
                // Reset stream position if possible
                if (stream.CanSeek && stream.Position > 0)
                {
                    stream.Position = 0;
                }
                
                await using (var fs = File.Create(path))
                {
                    await stream.CopyToAsync(fs);
                }
                return $"/uploads/images/{name}";
            }

            var extension2 = Path.GetExtension(fileName);
            if (string.IsNullOrWhiteSpace(extension2))
            {
                extension2 = contentType?.ToLowerInvariant() switch
                {
                    "image/jpeg" or "image/jpg" => ".jpg",
                    "image/png" => ".png",
                    "image/webp" => ".webp",
                    _ => ".jpg"
                };
            }
            
            var blobName = $"{Guid.NewGuid():N}{extension2}";
            var blob = _containerClient.GetBlobClient(blobName);
            
            // Reset stream position if possible
            if (stream.CanSeek && stream.Position > 0)
            {
                stream.Position = 0;
            }
            
            await blob.UploadAsync(stream, new BlobHttpHeaders { ContentType = contentType });
            
            if (blob.CanGenerateSasUri)
            {
                var sas = blob.GenerateSasUri(new BlobSasBuilder(BlobSasPermissions.Read, DateTimeOffset.UtcNow.AddDays(1)) { BlobContainerName = _container, BlobName = blobName });
                return sas.ToString();
            }
            return blob.Uri.ToString();
        }
    }
}


