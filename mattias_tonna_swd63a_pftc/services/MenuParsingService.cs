using System.Text.RegularExpressions;
using mattias_tonna_swd63a_pftc.Models;

namespace mattias_tonna_swd63a_pftc.services;

public class MenuParsingService
{
    public List<ParsedMenuItem> ParseMenuItems(string ocrText)
    {
        var items = new List<ParsedMenuItem>();

        var lines = ocrText
            .Split('\n')
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        for (int i = 0; i < lines.Count; i++)
        {
            var priceMatch = System.Text.RegularExpressions.Regex.Match(lines[i], @"€\s?(\d+[.,]\d{2})");

            if (!priceMatch.Success)
                continue;

            var priceText = priceMatch.Groups[1].Value.Replace(",", ".");
            var price = decimal.Parse(priceText, System.Globalization.CultureInfo.InvariantCulture);

            var possibleName = lines[i].Replace(priceMatch.Value, "").Trim();

            if (string.IsNullOrWhiteSpace(possibleName) && i > 0)
                possibleName = lines[i - 1].Trim();
            
            if (Regex.IsMatch(possibleName, @"^€?\d+[.,]\d{2}$"))
                continue;

            if (possibleName.Length < 3)
                continue;

            items.Add(new ParsedMenuItem
            {
                Name = possibleName,
                Price = price
            });
        }

        return items;
    }
}