using DubizzleScraper.Data;
using DubizzleScraper.Models;
using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace DubizzleScraper.Services;

public class DubizzleScraperService
{
    private readonly AppDbContext _db;
    private readonly ILogger<DubizzleScraperService> _logger;
    private readonly IConfiguration _config;
    private readonly HttpClient _http;
    private readonly int _maxPages;
    private readonly int _delayMs;

    public DubizzleScraperService(
        AppDbContext db,
        ILogger<DubizzleScraperService> logger,
        IConfiguration config,
        IHttpClientFactory httpClientFactory)
    {
        _db      = db;
        _logger  = logger;
        _config  = config;
        _http    = httpClientFactory.CreateClient("Dubizzle"); // erorr
        _maxPages = _config.GetValue<int>("Scraper:MaxPagesPerSearch", 3);
        _delayMs  = _config.GetValue<int>("Scraper:DelayBetweenRequestsMs", 2000);
    }

    // =================== نقطة الدخول الرئيسية ===================
    public async Task<List<Property>> ScrapeAllFiltersAsync()
    {
        var allNew = new List<Property>();

        var activeFilters = await _db.SearchFilters
            .Where(f => f.IsActive)
            .ToListAsync();

        if (!activeFilters.Any())
        {
            _logger.LogWarning("لا توجد فلاتر بحث نشطة في قاعدة البيانات.");
            return allNew;
        }

        foreach (var filter in activeFilters)
        {
            _logger.LogInformation("🔍 بحث عن: {Name}", filter.Name);
            try
            {
                var properties = await ScrapeFilterAsync(filter);
                var saved = await SaveNewPropertiesAsync(properties);
                allNew.AddRange(saved);
                _logger.LogInformation("✅ {Filter}: {Count} عقار جديد", filter.Name, saved.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ أثناء معالجة الفلتر: {Name}", filter.Name);
            }

            await Task.Delay(_delayMs);
        }

        return allNew;
    }

    // =================== Scrape فلتر واحد (كل صفحاته) ===================
    public async Task<List<Property>> ScrapeAllFiltersAsync()
    {
        var allNew = new List<Property>();

        // اقرأ الفلاتر من appsettings.json
        var filters = _config
            .GetSection("SearchFilters")
            .Get<List<SearchFilter>>();

        if (filters == null || !filters.Any())
        {
            _logger.LogWarning("لا توجد فلاتر في appsettings.json");
            return allNew;
        }

        foreach (var filter in filters)
        {
            _logger.LogInformation("🔍 بحث عن: {Name}", filter.Name);
            try
            {
                var properties = await ScrapeFilterAsync(filter);
                var saved = await SaveNewPropertiesAsync(properties);
                allNew.AddRange(saved);
                _logger.LogInformation("✅ {Filter}: {Count} عقار جديد", filter.Name, saved.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في الفلتر: {Name}", filter.Name);
            }

            await Task.Delay(_delayMs);
        }

        return allNew;
    }

    // =================== Scrape صفحة واحدة ===================
    private async Task<List<Property>> ScrapeSinglePageAsync(string url, PropertyType type)
    {
        var properties = new List<Property>();

        try
        {
            var html = await _http.GetStringAsync(url);
            var doc  = new HtmlDocument();
            doc.LoadHtml(html);

            // Dubizzle يستخدم بنية JSON-LD أو data attributes — نحاول كليهما
            var jsonLdNodes = doc.DocumentNode.SelectNodes("//script[@type='application/ld+json']");
            if (jsonLdNodes != null)
            {
                foreach (var node in jsonLdNodes)
                {
                    var parsed = TryParseJsonLd(node.InnerText, type, url);
                    if (parsed != null) properties.Add(parsed);
                }
            }

            // Fallback: HTML parsing للكروت
            if (!properties.Any())
            {
                var cards = doc.DocumentNode.SelectNodes(
                    "//article[contains(@class,'listing')]|//div[contains(@class,'_1eoqr26')]|//li[contains(@class,'listing')]");

                if (cards != null)
                {
                    foreach (var card in cards)
                    {
                        var prop = ParsePropertyCard(card, type, url);
                        if (prop != null) properties.Add(prop);
                    }
                }
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError("فشل الطلب HTTP: {Message}", ex.Message);
        }

        return properties;
    }

    // =================== تحليل JSON-LD ===================
    private Property? TryParseJsonLd(string json, PropertyType type, string pageUrl)
    {
        try
        {
            var obj = Newtonsoft.Json.Linq.JObject.Parse(json);
            if (obj["@type"]?.ToString() != "Product" &&
                obj["@type"]?.ToString() != "Residence") return null;

            var urlToken = obj["url"]?.ToString() ?? obj["@id"]?.ToString() ?? "";
            var id       = ExtractIdFromUrl(urlToken);
            if (string.IsNullOrEmpty(id)) return null;

            return new Property
            {
                DubizzleId  = id,
                Title       = obj["name"]?.ToString() ?? "عقار",
                Type        = type,
                Price       = decimal.TryParse(obj["offers"]?["price"]?.ToString(), out var p) ? p : null,
                Description = obj["description"]?.ToString(),
                Url         = urlToken,
                ImageUrl    = obj["image"]?.Type == Newtonsoft.Json.Linq.JTokenType.Array
                              ? obj["image"]?[0]?.ToString()
                              : obj["image"]?.ToString(),
                ScrapedAt   = DateTime.UtcNow
            };
        }
        catch { return null; }
    }

    // =================== تحليل HTML Card ===================
    private Property? ParsePropertyCard(HtmlNode card, PropertyType type, string pageUrl)
    {
        try
        {
            // استخرج الرابط
            var linkNode = card.SelectSingleNode(".//a[@href]");
            var href     = linkNode?.GetAttributeValue("href", "") ?? "";
            if (string.IsNullOrEmpty(href)) return null;

            var fullUrl = href.StartsWith("http") ? href : $"https://www.dubizzle.com.eg{href}";
            var id      = ExtractIdFromUrl(fullUrl);
            if (string.IsNullOrEmpty(id)) return null;

            // العنوان
            var title = card.SelectSingleNode(".//*[contains(@class,'title') or contains(@class,'Title')]")?.InnerText.Trim()
                     ?? card.SelectSingleNode(".//h2|.//h3")?.InnerText.Trim()
                     ?? "عقار";

            // السعر
            var priceText = card.SelectSingleNode(".//*[contains(@class,'price') or contains(@class,'Price')]")?.InnerText.Trim() ?? "";
            var price     = ParsePrice(priceText);

            // الموقع
            var location = card.SelectSingleNode(".//*[contains(@class,'location') or contains(@class,'Location')]")?.InnerText.Trim() ?? "";

            // الصورة
            var imgNode = card.SelectSingleNode(".//img[@src or @data-src]");
            var imgUrl  = imgNode?.GetAttributeValue("data-src", "")
                       ?? imgNode?.GetAttributeValue("src", "") ?? "";

            // تفاصيل (غرف، مساحة)
            var detailsText = card.InnerText;
            var bedrooms    = ExtractNumber(detailsText, new[] { "bed", "room", "غرف", "bedroom" });
            var bathrooms   = ExtractNumber(detailsText, new[] { "bath", "حمام" });
            var area        = ExtractArea(detailsText);

            return new Property
            {
                DubizzleId = id,
                Title      = CleanText(title),
                Type       = type,
                Price      = price,
                Location   = CleanText(location),
                Url        = fullUrl,
                ImageUrl   = imgUrl.StartsWith("http") ? imgUrl : null,
                Bedrooms   = bedrooms,
                Bathrooms  = bathrooms,
                AreaSqm    = area,
                ScrapedAt  = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogDebug("خطأ في تحليل الكارت: {Msg}", ex.Message);
            return null;
        }
    }

    // =================== حفظ العقارات الجديدة فقط ===================
    private async Task<List<Property>> SaveNewPropertiesAsync(List<Property> properties)
    {
        var newOnes = new List<Property>();

        foreach (var prop in properties)
        {
            if (string.IsNullOrEmpty(prop.DubizzleId)) continue;

            var exists = await _db.Properties
                .AnyAsync(p => p.DubizzleId == prop.DubizzleId);

            if (!exists)
            {
                _db.Properties.Add(prop);
                newOnes.Add(prop);
            }
        }

        if (newOnes.Any())
            await _db.SaveChangesAsync();

        return newOnes;
    }

    // =================== Helper Methods ===================
    private static string ExtractIdFromUrl(string url)
    {
        if (string.IsNullOrEmpty(url)) return string.Empty;
        var parts = url.TrimEnd('/').Split('/');
        // Dubizzle URLs end with: /some-title-ID/
        foreach (var part in parts.Reverse())
        {
            if (!string.IsNullOrEmpty(part) && part.Length > 5)
                return part;
        }
        return string.Empty;
    }

    private static decimal? ParsePrice(string text)
    {
        if (string.IsNullOrEmpty(text)) return null;
        var cleaned = System.Text.RegularExpressions.Regex.Replace(text, @"[^\d]", "");
        return decimal.TryParse(cleaned, out var val) ? val : null;
    }

    private static int? ExtractNumber(string text, string[] keywords)
    {
        foreach (var kw in keywords)
        {
            var match = System.Text.RegularExpressions.Regex.Match(
                text, $@"(\d+)\s*{kw}", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (match.Success && int.TryParse(match.Groups[1].Value, out var n))
                return n;
        }
        return null;
    }

    private static decimal? ExtractArea(string text)
    {
        var match = System.Text.RegularExpressions.Regex.Match(
            text, @"(\d[\d,]*)\s*(m²|sqm|متر|m2)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (match.Success)
        {
            var cleaned = match.Groups[1].Value.Replace(",", "");
            return decimal.TryParse(cleaned, out var v) ? v : null;
        }
        return null;
    }

    private static string CleanText(string text) =>
        System.Text.RegularExpressions.Regex.Replace(
            System.Net.WebUtility.HtmlDecode(text ?? ""), @"\s+", " ").Trim();
}
