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
                var dir = Path.Combine(_env.WebRootPath, "uploads");
                Directory.CreateDirectory(dir);
                var name = Guid.NewGuid().ToString("n") + Path.GetExtension(fileName);
                var path = Path.Combine(dir, name);
                using (var fs = File.Create(path))
                {
                    await stream.CopyToAsync(fs);
                }
                return $"/uploads/{name}";
            }

            var blobName = Guid.NewGuid().ToString("n") + Path.GetExtension(fileName);
            var blob = _containerClient.GetBlobClient(blobName);
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


