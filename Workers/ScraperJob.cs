using DubizzleScraper.Data;
using DubizzleScraper.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;

namespace DubizzleScraper.Workers;

[DisallowConcurrentExecution]
public class ScraperJob : IJob
{
    private readonly DubizzleScraperService _scraper;
    private readonly TelegramNotificationService _telegram;
    private readonly AppDbContext _db;
    private readonly ILogger<ScraperJob> _logger;

    public ScraperJob(
        DubizzleScraperService scraper,
        TelegramNotificationService telegram,
        AppDbContext db,
        ILogger<ScraperJob> logger)
    {
        _scraper  = scraper;
        _telegram = telegram;
        _db       = db;
        _logger   = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("⏰ بدء دورة Scraping: {Time}", DateTime.Now);

        try
        {
            // 1. Scrape وحفظ العقارات الجديدة
            var newProperties = await _scraper.ScrapeAllFiltersAsync();

            // 2. إرسال إشعار تليجرام لكل عقار جديد
            foreach (var property in newProperties)
            {
                try
                {
                    await _telegram.SendPropertyAsync(property);

                    // تحديث حالة الإشعار
                    property.NotificationSent = true;
                    await _db.SaveChangesAsync();

                    // تأخير بين الرسائل لتجنب rate limit
                    await Task.Delay(1000);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "فشل إرسال إشعار العقار: {Id}", property.DubizzleId);
                }
            }

            // 3. إرسال ملخص الدورة
            var totalCount = await _db.Properties.CountAsync();
            await _telegram.SendSummaryAsync(newProperties.Count, totalCount);

            _logger.LogInformation("✅ انتهت الدورة. جديد: {New}، إجمالي: {Total}",
                newProperties.Count, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ في دورة Scraping");
        }
    }
}
