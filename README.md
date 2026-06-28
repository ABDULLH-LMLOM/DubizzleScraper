# 🏠 Dubizzle Property Scraper

> يسكرايب عقارات من Dubizzle.com.eg تلقائياً، يخزّنها في SQL Server بدون تكرار، ويبعت إشعارات تليجرام فور ظهور عقار جديد.

---

## ⚙️ متطلبات التشغيل

| أداة | الإصدار |
|------|---------|
| .NET SDK | 8.0+ |
| SQL Server | 2019+ أو LocalDB |
| Telegram Bot | أي بوت تنشئه من @BotFather |

---

## 🚀 خطوات الإعداد

### 1. إنشاء Telegram Bot

1. افتح تليجرام وروح على **@BotFather**
2. ابعت `/newbot` واتبع التعليمات
3. احتفظ بـ **Bot Token**
4. ابعت رسالة للبوت، ثم افتح:
   ```
   https://api.telegram.org/botYOUR_TOKEN/getUpdates
   ```
5. احتفظ بـ **chat_id** من الـ JSON

### 2. إعداد appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=DubizzleScraperDB;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Telegram": {
    "BotToken": "1234567890:ABCDefGhIJKlmNoPQRsTUVwxyZ",
    "ChatId": "-1001234567890"
  },
  "Scraper": {
    "IntervalMinutes": 30,
    "MaxPagesPerSearch": 3,
    "DelayBetweenRequestsMs": 2000
  }
}
```

### 3. تثبيت الحزم وتشغيل Migrations

```bash
cd DubizzleScraper

# تثبيت الحزم
dotnet restore

# إنشاء قاعدة البيانات تلقائياً
dotnet ef database update

# تشغيل المشروع
dotnet run
```

> ✅ Migrations بتتطبق تلقائياً عند أول تشغيل.

---

## 🔍 إضافة فلاتر بحث جديدة

افتح SQL Server Management Studio أو أي أداة SQL وشغّل:

```sql
-- شقق للإيجار في الجيزة بسعر أقل من 10,000 جنيه
INSERT INTO SearchFilters (Name, PropertyType, Governorate, MaxPrice, IsActive, CreatedAt)
VALUES ('شقق إيجار الجيزة', 0, 'giza', 10000, 1, GETUTCDATE());

-- فيلات للبيع في 6 أكتوبر
INSERT INTO SearchFilters (Name, PropertyType, Governorate, District, IsActive, CreatedAt)
VALUES ('فيلات 6 أكتوبر', 2, 'giza', '6th-october', 1, GETUTCDATE());

-- شقق بيع 3 غرف في القاهرة الجديدة
INSERT INTO SearchFilters (Name, PropertyType, Governorate, MinBedrooms, MaxBedrooms, IsActive, CreatedAt)
VALUES ('شقق 3 غرف القاهرة الجديدة', 1, 'cairo', 3, 3, 1, GETUTCDATE());

-- محلات تجارية في المعادي
INSERT INTO SearchFilters (Name, PropertyType, District, IsActive, CreatedAt)
VALUES ('محلات المعادي', 4, 'maadi', 1, GETUTCDATE());
```

### أنواع العقارات (PropertyType)
| الرقم | النوع |
|-------|-------|
| 0 | شقة للإيجار |
| 1 | شقة للبيع |
| 2 | فيلا |
| 3 | أرض |
| 4 | محل تجاري |

### محافظات شائعة (Governorate)
| القيمة | المحافظة |
|--------|---------|
| `cairo` | القاهرة |
| `giza` | الجيزة |
| `alexandria` | الإسكندرية |
| `north-coast` | الساحل الشمالي |

---

## 🗃️ هيكل قاعدة البيانات

### جدول Properties
| العمود | النوع | الوصف |
|--------|-------|-------|
| DubizzleId | NVARCHAR(100) UNIQUE | معرّف فريد لمنع التكرار |
| Title | NVARCHAR(500) | عنوان الإعلان |
| Type | INT | نوع العقار |
| Price | DECIMAL | السعر |
| Bedrooms | INT | عدد الغرف |
| AreaSqm | DECIMAL | المساحة بالمتر |
| Location | NVARCHAR(200) | الموقع |
| Url | NVARCHAR(1000) | رابط الإعلان |
| NotificationSent | BIT | هل اتبعت الإشعار؟ |
| ScrapedAt | DATETIME2 | وقت الاستخراج |

### جدول SearchFilters
| العمود | النوع | الوصف |
|--------|-------|-------|
| Name | NVARCHAR(100) | اسم الفلتر |
| PropertyType | INT | نوع العقار |
| Governorate | NVARCHAR(100) | المحافظة |
| MinPrice / MaxPrice | DECIMAL | نطاق السعر |
| MinBedrooms / MaxBedrooms | INT | نطاق الغرف |
| IsActive | BIT | هل الفلتر نشط؟ |

---

## ⏰ تعديل وقت البحث

في `appsettings.json`:
```json
"Scraper": {
  "IntervalMinutes": 15   // ← كل ربع ساعة
}
```

| القيمة | التكرار |
|--------|---------|
| 15 | كل ربع ساعة |
| 30 | كل نص ساعة |
| 60 | كل ساعة |
| 120 | كل ساعتين |

---

## 📊 استعلامات مفيدة

```sql
-- آخر 10 عقارات مُضافة
SELECT TOP 10 * FROM Properties ORDER BY ScrapedAt DESC;

-- عقارات لم يُبعَت لها إشعار بعد
SELECT * FROM Properties WHERE NotificationSent = 0;

-- إحصاء حسب النوع
SELECT Type, COUNT(*) AS Count FROM Properties GROUP BY Type;

-- حذف عقارات أقدم من 30 يوم
DELETE FROM Properties WHERE ScrapedAt < DATEADD(DAY, -30, GETUTCDATE());
```

---

## 🐛 حل المشاكل الشائعة

**البوت مش بيبعت:**
- تأكد من صحة `BotToken` و`ChatId`
- لو ChatId لـ Group، لازم يبدأ بـ `-100`

**مش بيلاقي عقارات:**
- Dubizzle ممكن يغير الـ HTML structure
- جرّب `UseHeadlessBrowser: true` في appsettings (يحتاج PuppeteerSharp + Chromium)

**خطأ في الاتصال بـ SQL:**
- تأكد من صحة الـ Connection String
- لـ LocalDB: `Server=(localdb)\\mssqllocaldb;Database=DubizzleScraperDB;Trusted_Connection=True;`
