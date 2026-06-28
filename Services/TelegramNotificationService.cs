using DubizzleScraper.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace DubizzleScraper.Services;

public class TelegramNotificationService
{
    private readonly ITelegramBotClient _bot;
    private readonly string _chatId;
    private readonly ILogger<TelegramNotificationService> _logger;

    public TelegramNotificationService(
        IConfiguration config,
        ILogger<TelegramNotificationService> logger)
    {
        _logger = logger;
        var token = config["Telegram:BotToken"]
            ?? throw new InvalidOperationException("Telegram:BotToken مش موجود في appsettings.json");
        _chatId = config["Telegram:ChatId"]
            ?? throw new InvalidOperationException("Telegram:ChatId مش موجود في appsettings.json");

        _bot = new TelegramBotClient(token);
    }

    // =================== إرسال عقار واحد ===================
    public async Task SendPropertyAsync(Property property)
    {
        try
        {
            var message = FormatPropertyMessage(property);

            // لو في صورة — نبعتها مع النص
            if (!string.IsNullOrEmpty(property.ImageUrl))
            {
                try
                {
                    await _bot.SendPhotoAsync(
                        chatId: _chatId,
                        photo: new Telegram.Bot.Types.InputFileUrl(new Uri(property.ImageUrl)),
                        caption: message,
                        parseMode: ParseMode.Html
                    );
                    return;
                }
                catch
                {
                    // لو الصورة فشلت، نبعت نص فقط
                }
            }

            await _bot.SendTextMessageAsync(
                chatId: _chatId,
                text: message,
                parseMode: ParseMode.Html,
                disableWebPagePreview: false
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "فشل إرسال إشعار التليجرام للعقار: {Id}", property.DubizzleId);
            throw;
        }
    }

    // =================== إرسال ملخص دوري ===================
    public async Task SendSummaryAsync(int newCount, int totalCount)
    {
        var emoji = newCount > 0 ? "🎉" : "😴";
        var message = $"{emoji} <b>تقرير Dubizzle</b>\n\n" +
                      $"🆕 عقارات جديدة: <b>{newCount}</b>\n" +
                      $"📦 إجمالي في الداتابيز: <b>{totalCount}</b>\n" +
                      $"🕐 {DateTime.Now:dd/MM/yyyy HH:mm}";

        await _bot.SendTextMessageAsync(
            chatId: _chatId,
            text: message,
            parseMode: ParseMode.Html
        );
    }

    // =================== رسالة ترحيب عند البدء ===================
    public async Task SendStartupMessageAsync()
    {
        var message = "🚀 <b>Dubizzle Scraper بدأ يشتغل!</b>\n\n" +
                      "هيبعتلك إشعار فور ما يلاقي عقارات جديدة 🏠";

        await _bot.SendTextMessageAsync(
            chatId: _chatId,
            text: message,
            parseMode: ParseMode.Html
        );
    }

    // =================== تنسيق رسالة العقار ===================
    private static string FormatPropertyMessage(Property property)
    {
        var typeEmoji = property.Type switch
        {
            PropertyType.ApartmentForRent  => "🏢",
            PropertyType.ApartmentForSale  => "🏠",
            PropertyType.Villa             => "🏡",
            PropertyType.Land              => "🌿",
            PropertyType.CommercialShop    => "🏪",
            _ => "🏠"
        };

        var typeText = property.Type switch
        {
            PropertyType.ApartmentForRent  => "شقة للإيجار",
            PropertyType.ApartmentForSale  => "شقة للبيع",
            PropertyType.Villa             => "فيلا",
            PropertyType.Land              => "أرض",
            PropertyType.CommercialShop    => "محل تجاري",
            _ => "عقار"
        };

        var lines = new List<string>
        {
            $"{typeEmoji} <b>عقار جديد - {typeText}</b>",
            "",
            $"📌 <b>{HtmlEncode(property.Title)}</b>",
        };

        if (property.Price.HasValue)
        {
            var priceStr = property.Price.Value.ToString("N0");
            var period   = property.PricePeriod != null ? $" / {property.PricePeriod}" : "";
            lines.Add($"💰 السعر: <b>{priceStr} جنيه{period}</b>");
        }

        if (!string.IsNullOrEmpty(property.Location))
            lines.Add($"📍 الموقع: {HtmlEncode(property.Location)}");

        if (property.Bedrooms.HasValue)
            lines.Add($"🛏 غرف: {property.Bedrooms}");

        if (property.Bathrooms.HasValue)
            lines.Add($"🚿 حمامات: {property.Bathrooms}");

        if (property.AreaSqm.HasValue)
            lines.Add($"📐 المساحة: {property.AreaSqm} م²");

        if (!string.IsNullOrEmpty(property.SellerName))
            lines.Add($"👤 البائع: {HtmlEncode(property.SellerName)}");

        if (!string.IsNullOrEmpty(property.SellerType))
            lines.Add($"🏷 نوع: {(property.SellerType == "agent" ? "وسيط" : "مالك مباشر")}");

        if (!string.IsNullOrEmpty(property.Description))
        {
            var shortDesc = property.Description.Length > 200
                ? property.Description[..200] + "..."
                : property.Description;
            lines.Add($"\n📝 {HtmlEncode(shortDesc)}");
        }

        if (!string.IsNullOrEmpty(property.Url))
            lines.Add($"\n🔗 <a href='{property.Url}'>عرض العقار على Dubizzle</a>");

        lines.Add($"\n⏰ {DateTime.Now:dd/MM/yyyy HH:mm}");

        return string.Join("\n", lines);
    }

    private static string HtmlEncode(string text) =>
        System.Net.WebUtility.HtmlEncode(text ?? "");
}
