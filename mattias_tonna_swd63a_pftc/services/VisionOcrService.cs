using Google.Apis.Auth.OAuth2;
using Google.Cloud.Vision.V1;

namespace mattias_tonna_swd63a_pftc.services;

public class VisionOcrService
{
    private readonly ImageAnnotatorClient _client;

    public VisionOcrService(IConfiguration config)
    {
        var credentialsPath = config["Authentication:Google:CredentialsPath"];

        _client = new ImageAnnotatorClientBuilder
        {
            Credential = GoogleCredential.FromFile(credentialsPath)
        }.Build();
    }

    public async Task<string> ExtractTextAsync(IFormFile file)
    {
        await using var stream = file.OpenReadStream();

        var image = await Image.FromStreamAsync(stream);
        var result = await _client.DetectDocumentTextAsync(image);

        return result?.Text ?? "";
    }
}