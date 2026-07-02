# مستند فرانت‌اند: داشبورد مشاور

## جریان ثبت گزارش تماس

`POST /api/Consultant/SubmitLeadCallReport`

پس از ثبت موفق گزارش:

- دیالوگ رزرو **نباید** به‌صورت خودکار باز شود.
- اگر `canCreateReservation=true` بود، فقط دکمه «ثبت رزرو» در ردیف همان لید نمایش داده شود.
- فیلد `shouldOpenReservationPage` فقط برای سازگاری با نسخه‌های قبلی باقی مانده و نباید باعث باز شدن خودکار دیالوگ شود.

## وضعیت آنلاین/آفلاین پس از ثبت گزارش

بک‌اند پس از ثبت گزارش، وضعیت آنلاین مشاور را به‌صورت خودکار تنظیم می‌کند:

1. اگر لید آفلاین تعیین‌تکلیف‌نشده وجود داشته باشد → مشاور آفلاین می‌ماند.
2. اگر خارج از ساعت کاری (۹ تا ۲۱) باشد → مشاور آفلاین می‌ماند.
3. در غیر این صورت → مشاور آنلاین می‌شود.

فرانت باید:

- `GET /api/Consultant/GetDashboardStatus?profileId={id}` را دوباره فراخوانی کند.
- `GET /api/Consultant/GetLeads` را دوباره فراخوانی کند تا دکمه «ثبت گزارش» غیرفعال شود.
- اگر `autoOnlineApplied=false` و `autoOnlineBlockedReason` مقدار داشت، توستر هشدار نمایش دهد.

## فیلدهای جدید پاسخ ثبت گزارش

```json
{
  "isSuccess": true,
  "message": "گزارش با موفقیت ثبت شد. تا زمان تعیین تکلیف لیدهای آفلاین، امکان آنلاین شدن وجود ندارد",
  "data": {
    "leadAssignmentId": 1,
    "consultantProfileId": 10,
    "isReportSubmitted": true,
    "reportSubmittedAt": "2026-07-02T10:15:00",
    "leadAssignmentState": 3,
    "callResult": 1,
    "isConsultantOnline": false,
    "shouldOpenReservationPage": true,
    "canCreateReservation": true,
    "autoOnlineApplied": false,
    "autoOnlineBlockedReason": "تا زمان تعیین تکلیف لیدهای آفلاین، امکان آنلاین شدن وجود ندارد"
  }
}
```

## فیلدهای جدید لیست لیدها

`GET /api/Consultant/GetLeads`

هر آیتم علاوه بر فیلدهای قبلی شامل موارد زیر است:

- `reportSubmittedAt`
- `callResult`
- `isReportSubmitted`

## وضعیت داشبورد مشاور

`GET /api/Consultant/GetDashboardStatus?profileId={id}`

برای غیرفعال کردن دکمه آنلاین و نمایش پیام مناسب از فیلدهای زیر استفاده کنید:

- `canGoOnline`
- `onlineStatusBlockReason`
- `pendingOfflineLeadCount`

## ثبت رزرو

`POST /api/Reservation`

فقط زمانی که کاربر روی دکمه «ثبت رزرو» کلیک کرد دیالوگ رزرو باز شود.

```json
{
  "leadAssignmentId": 1,
  "consultantProfileId": 10,
  "reservationAt": "2026-07-02T12:00:00",
  "description": "توضیح رزرو"
}
```
