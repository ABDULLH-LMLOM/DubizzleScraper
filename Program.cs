using DubizzleScraper.Data;
using DubizzleScraper.Services;
using DubizzleScraper.Workers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quartz;

// ============================================================
//  DUBIZZLE PROPERTY SCRAPER
//  يعمل كـ Background Service — يسكرايب + يخزن + يبعت تليجرام
// ============================================================

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((ctx, cfg) =>
    {
        cfg.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
    })
    .ConfigureServices((ctx, services) =>
    {
        var config = ctx.Configuration;

        // ── قاعدة البيانات ──────────────────────────────────────
        services.AddDbContext<AppDbContext>(opt =>
            opt.UseSqlServer(config.GetConnectionString("DefaultConnection")));

        // ── HttpClient مع Headers تحاكي المتصفح ─────────────────
        services.AddHttpClient("Dubizzle", client =>
        {
            client.DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) " +
                "AppleWebKit/537.36 (KHTML, like Gecko) " +
                "Chrome/124.0.0.0 Safari/537.36");
            client.DefaultRequestHeaders.Add("Accept",
                "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
            client.DefaultRequestHeaders.Add("Accept-Language", "ar,en-US;q=0.9");
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // ── الـ Services ─────────────────────────────────────────
        services.AddScoped<DubizzleScraperService>();
        services.AddScoped<TelegramNotificationService>();
        services.AddScoped<FilterManagerService>();
        services.AddScoped<ScraperJob>();

        // ── Quartz (الجدولة الزمنية) ─────────────────────────────
        var intervalMinutes = config.GetValue<int>("Scraper:IntervalMinutes", 30);

        services.AddQuartz(q =>
        {
            q.UseMicrosoftDependencyInjectionJobFactory();

            var jobKey = new JobKey("ScraperJob");

            q.AddJob<ScraperJob>(opts => opts.WithIdentity(jobKey));

            q.AddTrigger(opts => opts
                .ForJob(jobKey)
                .WithIdentity("ScraperTrigger")
                .StartNow()                          // يشتغل فوراً عند البدء
                .WithSimpleSchedule(x => x
                    .WithIntervalInMinutes(intervalMinutes)
                    .RepeatForever())
            );
        });

        services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);
    })
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
        logging.SetMinimumLevel(LogLevel.Information);
    })
    .Build();

// ── تطبيق Migrations تلقائياً ──────────────────────────────
using (var scope = host.Services.CreateScope())
{
    var db     = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    logger.LogInformation("🔄 تطبيق Database Migrations...");
    await db.Database.MigrateAsync();
    logger.LogInformation("✅ Migrations جاهزة");

    // رسالة ترحيب على تليجرام
    try
    {
        var telegram = scope.ServiceProvider.GetRequiredService<TelegramNotificationService>();
        await telegram.SendStartupMessageAsync();
    }
    catch (Exception ex)
    {
        logger.LogWarning("لم يُرسَل إشعار البدء: {Msg}", ex.Message);
    }
}

Console.WriteLine("🚀 Dubizzle Scraper يعمل... اضغط Ctrl+C للإيقاف");
await host.RunAsync();
