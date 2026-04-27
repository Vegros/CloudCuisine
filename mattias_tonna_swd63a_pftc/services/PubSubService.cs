using Google.Apis.Auth.OAuth2;
using Google.Cloud.PubSub.V1;
using Google.Protobuf;
using Grpc.Auth;
using System.Text.Json;

namespace mattias_tonna_swd63a_pftc.services;

public class PubSubService
{
    private readonly PublisherClient _publisher;

    public PubSubService(IConfiguration config)
    {
        var projectId = config["Authentication:Google:ProjectId"];
        var topicId = config["PubSub:Google:TopicId"];
        var credentialsPath = config["Authentication:Google:CredentialsPath"];

        Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", credentialsPath);

        var topicName = TopicName.FromProjectTopic(projectId, topicId);

        _publisher = PublisherClient.CreateAsync(topicName).Result;
    }

    public async Task PublishMenuUploadAsync(string restaurantId, string menuId, string imageUrl, string fileName)
    {
        var payload = new
        {
            restaurantId,
            menuId,
            imageUrl,
            fileName
        };

        var json = JsonSerializer.Serialize(payload);
        
        var messageId = await _publisher.PublishAsync(new PubsubMessage
        {
            Data = ByteString.CopyFromUtf8(json)
        });

        Console.WriteLine($"Published Pub/Sub message: {messageId}");
    }
}