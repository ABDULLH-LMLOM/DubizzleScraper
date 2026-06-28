namespace DubizzleScraper.Models;

public class SearchFilter
{
    public string Name { get; set; } = string.Empty;
    public PropertyType PropertyType { get; set; }
    public string? Governorate { get; set; }
    public string? District { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public int? MinBedrooms { get; set; }
    public int? MaxBedrooms { get; set; }
    public decimal? MinArea { get; set; }
    public decimal? MaxArea { get; set; }
    public string? Keywords { get; set; }

    public string BuildDubizzleUrl()
    {
        var baseUrl = PropertyType switch
        {
            PropertyType.ApartmentForRent => "https://www.dubizzle.com.eg/properties/apartments-duplex-for-rent/",
            PropertyType.ApartmentForSale => "https://www.dubizzle.com.eg/properties/apartments-duplex-for-sale/",
            PropertyType.Villa => "https://www.dubizzle.com.eg/properties/villas-for-sale/",
            PropertyType.Land => "https://www.dubizzle.com.eg/properties/land-for-sale/",
            PropertyType.CommercialShop => "https://www.dubizzle.com.eg/properties/commercial-for-rent/",
            _ => "https://www.dubizzle.com.eg/properties/"
        };

        var queryParams = new List<string>();

        if (!string.IsNullOrWhiteSpace(Governorate))
            queryParams.Add($"location__governorate={Uri.EscapeDataString(Governorate)}");

        if (!string.IsNullOrWhiteSpace(District))
            queryParams.Add($"location__city={Uri.EscapeDataString(District)}");

        if (MinPrice.HasValue && MinPrice > 0)
            queryParams.Add($"price__gte={MinPrice}");

        if (MaxPrice.HasValue && MaxPrice > 0)
            queryParams.Add($"price__lte={MaxPrice}");

        if (MinBedrooms.HasValue && MinBedrooms > 0)
            queryParams.Add($"rooms__gte={MinBedrooms}");

        if (MaxBedrooms.HasValue && MaxBedrooms > 0)
            queryParams.Add($"rooms__lte={MaxBedrooms}");

        if (!string.IsNullOrWhiteSpace(Keywords))
            queryParams.Add($"q={Uri.EscapeDataString(Keywords)}");

        return queryParams.Count > 0
            ? $"{baseUrl}?{string.Join("&", queryParams)}"
            : baseUrl;
    }
}