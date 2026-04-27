using Google.Cloud.Firestore;

namespace mattias_tonna_swd63a_pftc.Models;

[FirestoreData]
public class Restaurant
{
    [FirestoreProperty]
    public string RestaurantId { get; set; } = "";

    [FirestoreProperty]
    public string Name { get; set; } = "";

    [FirestoreProperty]
    public string Status { get; set; } = "pending";

    [FirestoreProperty]
    public string CreatedBy { get; set; } = "";

    [FirestoreProperty]
    public Timestamp CreatedAt { get; set; } = Timestamp.GetCurrentTimestamp();
}