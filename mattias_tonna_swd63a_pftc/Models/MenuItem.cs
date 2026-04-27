using Google.Cloud.Firestore;

namespace mattias_tonna_swd63a_pftc.Models;

[FirestoreData]
public class MenuItem
{
    [FirestoreProperty]
    public string ItemId { get; set; } = "";

    [FirestoreProperty]
    public string Name { get; set; } = "";

    [FirestoreProperty]
    public double  Price { get; set; }

    [FirestoreProperty]
    public Timestamp CreatedAt { get; set; } = Timestamp.GetCurrentTimestamp();
}