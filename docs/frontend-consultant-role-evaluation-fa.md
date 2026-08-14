# راهنمای فرانت‌اند: ارزیابی نقش مشاور

برای نمایش وضعیت ارتقا/تنزل در پنل ادمین، پاسخ فعلی زیر را مصرف کنید:

```http
GET /api/admin/consultants/{profileId}
```

فیلد جدید `roleEvaluation` شامل این مقادیر است:

| فیلد | کاربرد |
|---|---|
| `currentRole` | نقش جاری (`Test`، `Seller` یا `TopSeller`؛ بسته به تنظیم serializer ممکن است عدد enum ارسال شود) |
| `periodStartedAt` | شروع دوره جاری به UTC |
| `nextEvaluationAt` | زمان ارزیابی بعدی به UTC |
| `successfulPatientCount` | تعداد بیماران یکتای تاییدشده توسط منشی در دوره جاری |
| `lastEvaluationResult` | نتیجه آخرین ارزیابی؛ برای مشاور ارزیابی‌نشده `null` است |
| `lastEvaluatedAt` | زمان آخرین ارزیابی به UTC |

مقادیر `dailyLimits.realtime` و `dailyLimits.burnt` نیز از Policy نقش جاری برمی‌گردند؛ بنابراین فرانت‌اند نباید سقف‌های ۲۰/۱۰/۳۰ یا طول دوره‌ها را هاردکد کند. در پنج روز دوم نقش Test، سقف Burnt برابر صفر و `canPickup` برابر `false` خواهد بود.

تغییر ضروری دیگری در فرانت‌اند وجود ندارد. برای نمایش بهتر می‌توان enum نتیجه را مطابق جدول زیر برچسب‌گذاری کرد: `Deactivated`، `PromotedToSeller`، `PromotedToTopSeller`، `RemainedSeller`، `DemotedToTest`، `TopSellerHigherReward`، `TopSellerReward`، `RemainedTopSeller` و `DemotedToSeller`.
