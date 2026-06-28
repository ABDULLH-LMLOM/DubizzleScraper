using DubizzleScraper.Data;
using DubizzleScraper.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DubizzleScraper.Services;

/// <summary>
/// خدمة إدارة فلاتر البحث — إضافة / تعديل / حذف / عرض
/// </summary>
public class FilterManagerService
{
    private readonly AppDbContext _db;
    private readonly ILogger<FilterManagerService> _logger;

    public FilterManagerService(AppDbContext db, ILogger<FilterManagerService> logger)
    {
        _db     = db;
        _logger = logger;
    }

    public async Task<List<SearchFilter>> GetAllAsync() =>
        await _db.SearchFilters.OrderBy(f => f.Id).ToListAsync();

    public async Task<SearchFilter?> GetByIdAsync(int id) =>
        await _db.SearchFilters.FindAsync(id);

    public async Task<SearchFilter> AddAsync(SearchFilter filter)
    {
        _db.SearchFilters.Add(filter);
        await _db.SaveChangesAsync();
        _logger.LogInformation("تم إضافة فلتر: {Name}", filter.Name);
        return filter;
    }

    public async Task UpdateAsync(SearchFilter filter)
    {
        _db.SearchFilters.Update(filter);
        await _db.SaveChangesAsync();
        _logger.LogInformation("تم تحديث فلتر: {Name}", filter.Name);
    }

    public async Task DeleteAsync(int id)
    {
        var filter = await _db.SearchFilters.FindAsync(id);
        if (filter != null)
        {
            _db.SearchFilters.Remove(filter);
            await _db.SaveChangesAsync();
            _logger.LogInformation("تم حذف فلتر: {Name}", filter.Name);
        }
    }

    public async Task ToggleActiveAsync(int id)
    {
        var filter = await _db.SearchFilters.FindAsync(id);
        if (filter != null)
        {
            filter.IsActive = !filter.IsActive;
            await _db.SaveChangesAsync();
            _logger.LogInformation("فلتر {Name}: {Status}", filter.Name,
                filter.IsActive ? "مفعّل" : "موقوف");
        }
    }

    /// <summary>
    /// طباعة الرابط المُولَّد من الفلتر للتحقق منه قبل التشغيل
    /// </summary>
    public async Task PrintFilterUrlsAsync()
    {
        var filters = await GetAllAsync();
        Console.WriteLine("\n===== روابط الفلاتر =====");
        foreach (var f in filters)
        {
            Console.WriteLine($"\n[{f.Id}] {f.Name} ({(f.IsActive ? "نشط" : "موقوف")})");
            Console.WriteLine($"  URL: {f.BuildDubizzleUrl()}");
        }
        Console.WriteLine("==========================\n");
    }
}
