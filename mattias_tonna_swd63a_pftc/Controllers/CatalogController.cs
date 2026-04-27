using mattias_tonna_swd63a_pftc.DataAccess;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace mattias_tonna_swd63a_pftc.Controllers;

[Authorize]
public class CatalogController : Controller
{
    private readonly MenuRepository _menuRepository;

    public CatalogController(MenuRepository menuRepository)
    {
        _menuRepository = menuRepository;
    }

    public async Task<IActionResult> Index()
    {
        var items = await _menuRepository.GetCatalogItemsAsync();
        return View(items);
    }
}