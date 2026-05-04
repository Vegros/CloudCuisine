using mattias_tonna_swd63a_pftc.DataAccess;
using mattias_tonna_swd63a_pftc.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Google.Apis.Auth.OAuth2;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace mattias_tonna_swd63a_pftc.Controllers;

[Authorize]
public class CatalogController : Controller
{
    private readonly MenuRepository _menuRepository;
    private readonly HttpClient _httpClient;
    private readonly ILogger<CatalogController> _logger;

    public CatalogController(MenuRepository menuRepository, HttpClient httpClient, ILogger<CatalogController> logger)
    {
        _menuRepository = menuRepository;
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var items = await _menuRepository.GetCatalogItemsAsync();
        ViewBag.PendingCount = await _menuRepository.GetPendingMenuCountAsync();
        return View(items);
    }
    
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Translate([FromBody] TranslateRequest request)
    {
        var translateUrl = "https://translate-248102223811.europe-west1.run.app";

        var credential = await GoogleCredential.GetApplicationDefaultAsync();

        var oidcToken = await credential
            .GetOidcTokenAsync(OidcTokenOptions.FromTargetAudience(translateUrl));

        var token = await oidcToken.GetAccessTokenAsync();

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, translateUrl)
        {
            Content = JsonContent.Create(request)
        };

        httpRequest.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(httpRequest);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            return StatusCode((int)response.StatusCode, error);
        }

        var result = await response.Content.ReadFromJsonAsync<object>();
        return Json(result);
    }
}