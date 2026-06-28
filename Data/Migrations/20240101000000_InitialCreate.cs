using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DubizzleScraper.Data.Migrations;

/// <inheritdoc />
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "SearchFilters",
            columns: table => new
            {
                Id          = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                Name        = table.Column<string>(maxLength: 100, nullable: false),
                PropertyType = table.Column<int>(nullable: false),
                Governorate = table.Column<string>(maxLength: 100, nullable: true),
                District    = table.Column<string>(maxLength: 100, nullable: true),
                MinPrice    = table.Column<decimal>(nullable: true),
                MaxPrice    = table.Column<decimal>(nullable: true),
                MinBedrooms = table.Column<int>(nullable: true),
                MaxBedrooms = table.Column<int>(nullable: true),
                MinArea     = table.Column<decimal>(nullable: true),
                MaxArea     = table.Column<decimal>(nullable: true),
                Keywords    = table.Column<string>(maxLength: 200, nullable: true),
                IsActive    = table.Column<bool>(nullable: false, defaultValue: true),
                CreatedAt   = table.Column<DateTime>(nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_SearchFilters", x => x.Id));

        migrationBuilder.CreateTable(
            name: "Properties",
            columns: table => new
            {
                Id               = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                DubizzleId       = table.Column<string>(maxLength: 100, nullable: false),
                Title            = table.Column<string>(maxLength: 500, nullable: false),
                Type             = table.Column<int>(nullable: false),
                Price            = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                PricePeriod      = table.Column<string>(maxLength: 50, nullable: true),
                Bedrooms         = table.Column<int>(nullable: true),
                Bathrooms        = table.Column<int>(nullable: true),
                AreaSqm          = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                Location         = table.Column<string>(maxLength: 200, nullable: true),
                Governorate      = table.Column<string>(maxLength: 100, nullable: true),
                District         = table.Column<string>(maxLength: 100, nullable: true),
                Description      = table.Column<string>(maxLength: 2000, nullable: true),
                Url              = table.Column<string>(maxLength: 1000, nullable: true),
                ImageUrl         = table.Column<string>(maxLength: 1000, nullable: true),
                SellerName       = table.Column<string>(maxLength: 100, nullable: true),
                SellerPhone      = table.Column<string>(maxLength: 50, nullable: true),
                SellerType       = table.Column<string>(maxLength: 50, nullable: true),
                ScrapedAt        = table.Column<DateTime>(nullable: false),
                PostedAt         = table.Column<DateTime>(nullable: true),
                NotificationSent = table.Column<bool>(nullable: false, defaultValue: false),
                IsActive         = table.Column<bool>(nullable: false, defaultValue: true)
            },
            constraints: table => table.PrimaryKey("PK_Properties", x => x.Id));

        migrationBuilder.CreateIndex(
            name: "IX_Properties_DubizzleId",
            table: "Properties",
            column: "DubizzleId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Properties_NotificationSent",
            table: "Properties",
            column: "NotificationSent");

        migrationBuilder.CreateIndex(
            name: "IX_Properties_ScrapedAt",
            table: "Properties",
            column: "ScrapedAt");

        // Seed Data
        migrationBuilder.InsertData(
            table: "SearchFilters",
            columns: new[] { "Name", "PropertyType", "Governorate", "IsActive", "CreatedAt" },
            values: new object[,]
            {
                { "شقق إيجار القاهرة", 0, "cairo", true, new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                { "شقق بيع القاهرة",   1, "cairo", true, new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "Properties");
        migrationBuilder.DropTable(name: "SearchFilters");
    }
}
