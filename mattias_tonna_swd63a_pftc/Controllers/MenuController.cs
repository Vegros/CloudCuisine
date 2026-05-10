using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using mattias_tonna_swd63a_pftc.interfaces;
using mattias_tonna_swd63a_pftc.DataAccess;
using mattias_tonna_swd63a_pftc.Models;
using mattias_tonna_swd63a_pftc.services;
using System.Net.Http;
using System.Net.Http.Json;
using Google.Apis.Auth.OAuth2;

using System.Net.Http.Headers;
namespace mattias_tonna_swd63a_pftc.Controllers;


[Authorize]
public class MenuController : Controller
{
    private readonly MenuRepository _menuRepository;
    private readonly IBucketStorageService _bucketStorageService;
    private readonly PubSubService _pubSubService;
    private readonly HttpClient _httpClient;
    private readonly ILogger<MenuController> _logger;

    public MenuController(
        IBucketStorageService bucketStorageService,
        MenuRepository menuRepository,
        PubSubService pubSubService,
        HttpClient httpClient,
        ILogger<MenuController> logger)
    {
        _bucketStorageService = bucketStorageService;
        _menuRepository = menuRepository;
        _pubSubService = pubSubService;
        _httpClient = httpClient;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Upload()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Upload(List<IFormFile>? files, string restaurantName)
    {
        if (files == null || files.Count == 0)
        {
            ViewBag.Message = "Please select at least one image.";
            return View();
        }

        try
        {
            foreach (var file in files)
            {
                var fileNameForStorage =
                    $"menus/{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";

                var imageUrl = await _bucketStorageService.UploadFileAsync(file, fileNameForStorage);
                
                
                var uploadResult = await _menuRepository.SaveMenuUploadAsync(
                    restaurantName,
                    imageUrl,
                    fileNameForStorage,
                    User.Identity?.Name ?? "unknown",
                    null,
                    new List<ParsedMenuItem>()
                );
                _logger.LogInformation("Publishing message to Pub/Sub for menu {MenuId}", uploadResult.MenuId);

                await _pubSubService.PublishMenuUploadAsync(
                    uploadResult.RestaurantId,
                    uploadResult.MenuId,
                    imageUrl,
                    fileNameForStorage
                );
                
                _logger.LogInformation("Published message to Pub/Sub successfully");
                
                try
                {
                    var clearCacheUrl = "https://translate-248102223811.europe-west1.run.app/clear_cache";

                    var credential = await GoogleCredential.GetApplicationDefaultAsync();

                    var oidcToken = await credential.GetOidcTokenAsync(
                        OidcTokenOptions.FromTargetAudience("https://translate-248102223811.europe-west1.run.app")
                    );

                    var token = await oidcToken.GetAccessTokenAsync();

                    var request = new HttpRequestMessage(HttpMethod.Post, clearCacheUrl)
                    {
                        Content = JsonContent.Create(new
                        {
                            restaurantId = uploadResult.RestaurantId
                        })
                    };

                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                    var response = await _httpClient.SendAsync(request);

                    _logger.LogInformation("Cache clear response: {StatusCode}", response.StatusCode);

                    if (!response.IsSuccessStatusCode)
                    {
                        var body = await response.Content.ReadAsStringAsync();
                        _logger.LogWarning("Cache clear failed: {StatusCode} {Body}", response.StatusCode, body);
                    }
                }
                catch (Exception cacheEx)
                {
                    _logger.LogWarning(cacheEx, "Failed to clear translation cache");
                }
                
            
            }
            
            ViewBag.Message = $"{files.Count} file(s) uploaded. OCR processing started.";
            

            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Menu image upload failed");
            ViewBag.Message = ex.Message;
            return View();
        }
    }
}