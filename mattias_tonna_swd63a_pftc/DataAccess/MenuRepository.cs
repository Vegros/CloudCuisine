using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using mattias_tonna_swd63a_pftc.Models;

namespace mattias_tonna_swd63a_pftc.DataAccess;

public class MenuRepository
{
    private readonly ILogger<MenuRepository> _logger;
    private readonly FirestoreDb _db;

    public MenuRepository(ILogger<MenuRepository> logger, IConfiguration config)
    {
        _logger = logger;
        var projectId = config["Authentication:Google:ProjectId"];
        var credentialsPath = config["Authentication:Google:CredentialsPath"];

        _db = new FirestoreDbBuilder
        {
            ProjectId = projectId,
            DatabaseId = config["Storage:Google:DatabaseId"],
            Credential = GoogleCredential.FromFile(credentialsPath)
        }.Build();
    }

    public async Task<MenuUploadResult> SaveMenuUploadAsync(
        string restaurantName,
        string imageUrl,
        string fileName,
        string uploadedBy,
        string ocrText, 
        List<ParsedMenuItem> parsedItems)
    {
        var restaurantQuery = await _db.Collection("restaurants")
            .WhereEqualTo("Name", restaurantName)
            .Limit(1)
            .GetSnapshotAsync();

        DocumentReference restaurantRef;

        if (restaurantQuery.Documents.Count > 0)
        {
            restaurantRef = restaurantQuery.Documents[0].Reference;
        }
        else
        {
            restaurantRef = _db.Collection("restaurants").Document();

            var restaurant = new Restaurant
            {
                RestaurantId = restaurantRef.Id,
                Name = restaurantName,
                Status = "pending",
                CreatedBy = uploadedBy,
                CreatedAt = Timestamp.GetCurrentTimestamp()
            };

            await restaurantRef.SetAsync(restaurant);
        }
        
        var menuRef = restaurantRef.Collection("menus").Document();
        var imageRef = menuRef.Collection("images").Document();

        var menu = new Menu
        {
            MenuId = menuRef.Id,
            Status = "pending",
            OcrText = ocrText,
            CreatedAt = Timestamp.GetCurrentTimestamp()
        };

        var image = new MenuImage
        {
            ImageId = imageRef.Id,
            ImageUrl = imageUrl,
            FileName = fileName,
            UploadedBy = uploadedBy,
            UploadedAt = Timestamp.GetCurrentTimestamp()
        };
        
        await menuRef.SetAsync(menu);
        await imageRef.SetAsync(image);
        foreach (var parsedItem in parsedItems)
        {
            var itemRef = menuRef.Collection("items").Document();
            
            var menuItem = new MenuItem
            {
                ItemId = itemRef.Id,
                Name = parsedItem.Name,
                Price = Convert.ToDouble(parsedItem.Price),
                CreatedAt = Timestamp.GetCurrentTimestamp()
            };

            await itemRef.SetAsync(menuItem);
        }
        _logger.LogInformation("Menu uploaded for restaurant {Name}", restaurantName);
        return new MenuUploadResult
        {
            RestaurantId = restaurantRef.Id,
            MenuId = menuRef.Id
        };
        
    }
    
    public async Task<List<CatalogItem>> GetCatalogItemsAsync()
    {
        var results = new List<CatalogItem>();

        var restaurantsSnapshot = await _db.Collection("restaurants").GetSnapshotAsync();

        foreach (var restaurantDoc in restaurantsSnapshot.Documents)
        {
            var menusSnapshot = await restaurantDoc.Reference
                .Collection("menus")
                .GetSnapshotAsync();

            foreach (var menuDoc in menusSnapshot.Documents)
            {
                var menu = menuDoc.ConvertTo<Menu>();

                if (menu.Status != "completed")
                    continue;

                var itemsSnapshot = await menuDoc.Reference
                    .Collection("items")
                    .GetSnapshotAsync();

                foreach (var itemDoc in itemsSnapshot.Documents)
                {
                    var item = itemDoc.ConvertTo<MenuItem>();
                    var restaurant = restaurantDoc.ConvertTo<Restaurant>();
                    
                    results.Add(new CatalogItem
                    {
                        Name = item.Name,
                        Price = item.Price,
                        RestaurantName = restaurant.Name,
                    });
                }
            }
        }

        return results;
    }
    
    public async Task<int> GetPendingMenuCountAsync()
    {
        var pendingCount = 0;
        var restaurantsSnapshot = await _db.Collection("restaurants").GetSnapshotAsync();

        foreach (var restaurantDoc in restaurantsSnapshot.Documents)
        {
            var menusSnapshot = await restaurantDoc.Reference
                .Collection("menus")
                .GetSnapshotAsync();

            foreach (var menuDoc in menusSnapshot.Documents)
            {
                var menu = menuDoc.ConvertTo<Menu>();

                if (menu.Status == "pending")
                    pendingCount++;
            }
        }

        return pendingCount;
    }
}