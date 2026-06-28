using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DubizzleScraper.Models;

public enum PropertyType
{
    ApartmentForRent,
    ApartmentForSale,
    Villa,
    Land,
    CommercialShop
}

public class Property
{
    [Key]
    public int Id { get; set; }

    // المعرّف الفريد من dubizzle (يُستخدم لمنع التكرار)
    [Required]
    [MaxLength(100)]
    public string DubizzleId { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Title { get; set; } = string.Empty;

    public PropertyType Type { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? Price { get; set; }

    [MaxLength(50)]
    public string? PricePeriod { get; set; } // monthly / yearly / total

    public int? Bedrooms { get; set; }

    public int? Bathrooms { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? AreaSqm { get; set; }

    [MaxLength(200)]
    public string? Location { get; set; }

    [MaxLength(100)]
    public string? Governorate { get; set; }

    [MaxLength(100)]
    public string? District { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }

    [MaxLength(1000)]
    public string? Url { get; set; }

    [MaxLength(1000)]
    public string? ImageUrl { get; set; }

    [MaxLength(100)]
    public string? SellerName { get; set; }

    [MaxLength(50)]
    public string? SellerPhone { get; set; }

    [MaxLength(50)]
    public string? SellerType { get; set; } // agent / owner

    public DateTime ScrapedAt { get; set; } = DateTime.UtcNow;

    public DateTime? PostedAt { get; set; }

    public bool NotificationSent { get; set; } = false;

    public bool IsActive { get; set; } = true;
}
