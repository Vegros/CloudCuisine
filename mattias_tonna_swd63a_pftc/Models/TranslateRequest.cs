namespace mattias_tonna_swd63a_pftc.Models;

public class TranslateRequest
{
    public string Text { get; set; } = "";

    public string Target { get; set; } = "en";

    public string RestaurantId { get; set; } = "global";
}