using Google.Cloud.Firestore;

namespace mattias_tonna_swd63a_pftc.Models;

[FirestoreData]
public class MenuImage
{
    [FirestoreProperty]
    public string ImageId { get; set; } = "";

    [FirestoreProperty]
    public string ImageUrl { get; set; } = "";

    [FirestoreProperty]
    public string FileName { get; set; } = "";

    [FirestoreProperty]
    public string UploadedBy { get; set; } = "";

    [FirestoreProperty]
    public Timestamp UploadedAt { get; set; } = Timestamp.GetCurrentTimestamp();
}