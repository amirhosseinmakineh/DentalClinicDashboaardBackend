# تغییر بک‌اند: گزارش همه رزروها در پنل ادمین

هر دو endpoint گزارش رزروهای روزانه ادمین پارامتر اختیاری boolean به نام
`includeAll` را می‌پذیرند:

```http
GET /api/admin/reports/daily-reservations?includeAll=true
GET /api/admin/reports/daily-reservations/export?includeAll=true
Authorization: Bearer <token>
```

وقتی مقدار این پارامتر `true` باشد، فیلترهای `date`، `consultantProfileId` و
`requestStatus` نادیده گرفته می‌شوند و همه رزروهای حذف‌نشده، با ترتیب نزولی
`createdAt` (و سپس شناسه رزرو)، در پاسخ یا فایل CSV قرار می‌گیرند. خلاصه گزارش
نیز از همین مجموعه کامل محاسبه می‌شود.

در این حالت مقدار `date` در پاسخ رشته خالی و مقدار `datePersian` برابر `null`
است. نام فایل خروجی `daily-reservations-all.csv` خواهد بود.

اگر `includeAll` ارسال نشود یا `false` باشد، رفتار قبلی حفظ می‌شود: تاریخ
ارسالی (یا امروز ایران) و فیلترهای اختیاری مشاور و وضعیت اعمال خواهند شد.

دسترسی هر دو endpoint همچنان فقط برای نقش `Admin` و با توکن معتبر امکان‌پذیر
است.
