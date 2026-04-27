using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using mattias_tonna_swd63a_pftc.interfaces;

namespace mattias_tonna_swd63a_pftc.services;

public class BucketStorageService:IBucketStorageService
{
    private readonly ILogger<BucketStorageService> _logger;
    private readonly string _bucketName;
    private readonly StorageClient _storageClient;

    public BucketStorageService(ILogger<BucketStorageService> logger, IConfiguration config)
    {
        _logger = logger;
        _bucketName = config.GetValue<string>("Storage:Google:BucketName");
        
        var credentialsPath = config.GetValue<string>("Authentication:Google:CredentialsPath");

        _storageClient = new StorageClientBuilder
        {
            Credential = GoogleCredential.FromFile(credentialsPath)
        }.Build();
    }

    public async Task<string> UploadFileAsync(IFormFile file, string fileNameForStorage)
    {
        if (file == null || file.Length == 0)
        {
            throw new ArgumentNullException(nameof(file), "File is empty or null");
        }
        
        string[] permittedExtensions = { ".jpg", ".jpeg", ".png", ".gif" };
        if (!permittedExtensions.Contains(Path.GetExtension(file.FileName).ToLowerInvariant()))
        {
            throw new ArgumentException(nameof(file), "File extension is not accepted. Allowed extensions are: " + 
                                                      string.Join(", ", permittedExtensions));
        }

        try
        {
            if (string.IsNullOrWhiteSpace(fileNameForStorage))
            {
                fileNameForStorage = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            }

            string contentType = file.ContentType;
            UploadObjectOptions options = new UploadObjectOptions();
            
            using (var memoryStream = new MemoryStream())
            {
                await file.CopyToAsync(memoryStream);
                memoryStream.Position = 0;
                
                //Upload the file to Google Cloud Storage
                var storageObject = await  _storageClient.UploadObjectAsync(_bucketName, fileNameForStorage, contentType, memoryStream, options);
                _logger.LogInformation($"Uploaded file {fileNameForStorage} to bucket {_bucketName}");
                return $"https://storage.googleapis.com/{_bucketName}/{fileNameForStorage}";
            }
        }
        catch (Google.GoogleApiException gae)
        {
            _logger.LogError(gae,$"Google API Error during file upload: {gae.Message}");
            throw new ApplicationException($"Google API Error during file upload: {gae.Message}");
        }
        catch (Exception e)
        {
            _logger.LogError($"Unexpected error during file upload: {e.Message}");
            throw new ApplicationException($"Unexpected error during file upload: {e.Message}");
        }
    }

    public Task DeleteFileAsync(string fileName)
    {
        throw new NotImplementedException();
    }
}