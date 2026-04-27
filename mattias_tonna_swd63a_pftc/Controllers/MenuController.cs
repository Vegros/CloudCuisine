using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using mattias_tonna_swd63a_pftc.interfaces;
using mattias_tonna_swd63a_pftc.DataAccess;
using mattias_tonna_swd63a_pftc.Models;
using mattias_tonna_swd63a_pftc.services;


namespace mattias_tonna_swd63a_pftc.Controllers;


[Authorize]
public class MenuController : Controller
{
    private readonly MenuRepository _menuRepository;
    private readonly IBucketStorageService _bucketStorageService;
    private readonly PubSubService _pubSubService;
    private readonly ILogger<MenuController> _logger;

    public MenuController(
        IBucketStorageService bucketStorageService,
        MenuRepository menuRepository,
        PubSubService pubSubService,
        ILogger<MenuController> logger)
    {
        _bucketStorageService = bucketStorageService;
        _menuRepository = menuRepository;
        _pubSubService = pubSubService;
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