using Google.Cloud.Firestore;

namespace mattias_tonna_swd63a_pftc.Models;

[FirestoreData]
public class Menu
{
    [FirestoreProperty]
    public string MenuId { get; set; } = "";

    [FirestoreProperty]
    public string Status { get; set; } = "pending";

    [FirestoreProperty]
    public string OcrText { get; set; } = "";

    [FirestoreProperty]
    public Timestamp CreatedAt { get; set; } = Timestamp.GetCurrentTimestamp();
}