# راهنمای فرانت‌اند: ستون «اعلام منشی» در لیست رزروها

## هدف

در جدول رزروهای ادمین/منشی یک ستون جدید با عنوان **اعلام منشی** نمایش دهید. منشی می‌تواند بعد از تماس با بیمار، نتیجه تماس را به‌صورت متن آزاد ثبت کند و در مراجعه‌های بعدی همان متن را ببیند یا ویرایش کند.

## دریافت لیست

از API فعلی زیر استفاده کنید:

```http
GET /api/Reservation/SecretaryReservations
```

هر آیتم لیست اکنون سه فیلد جدید دارد:

```ts
type SecretaryReservationItem = {
  // فیلدهای قبلی...
  secretaryAnnouncement: string | null;
  secretaryAnnouncementUpdatedAt: string | null; // ISO DateTime, UTC
  secretaryAnnouncementUserId: string | null;    // Guid
};
```

نمونه آیتم:

```json
{
  "id": 124,
  "patientName": "علی رضایی",
  "reservationAt": "2026-08-20T10:30:00",
  "secretaryAnnouncement": "تماس گرفته شد؛ بیمار حضور فردا را تأیید کرد.",
  "secretaryAnnouncementUpdatedAt": "2026-08-19T08:15:00Z",
  "secretaryAnnouncementUserId": "4df08440-29bf-4ce0-953f-26686f043f04"
}
```

## ثبت یا ویرایش اعلام منشی

```http
PUT /api/Reservation/SecretaryAnnouncement
Content-Type: application/json
```

```json
{
  "reservationId": 124,
  "secretaryUserId": "4df08440-29bf-4ce0-953f-26686f043f04",
  "secretaryAnnouncement": "تماس گرفته شد؛ بیمار حضور فردا را تأیید کرد."
}
```

- `reservationId` شناسه رزرو همان ردیف است.
- `secretaryUserId` شناسه کاربر منشی لاگین‌شده است.
- `secretaryAnnouncement` متن آزاد با حداکثر **۱۰۰۰ کاراکتر** است.
- ارسال `null`، رشته خالی یا فقط فاصله، متن قبلی را پاک می‌کند و زمان/کاربر آخرین تغییر همچنان ثبت می‌شود.
- رزرو لغوشده قابل ویرایش نیست.

پاسخ موفق از الگوی عمومی `Result` استفاده می‌کند:

```json
{
  "isSuccess": true,
  "message": "اعلام منشی با موفقیت ثبت شد"
}
```

## پیشنهاد پیاده‌سازی UI

1. بعد از ستون «زمان رزرو»، ستون «اعلام منشی» را اضافه کنید.
2. مقدار موجود را در یک textarea یا input قابل ویرایش نمایش دهید؛ `maxLength={1000}` تنظیم شود.
3. ذخیره را با دکمه مستقل همان ردیف یا هنگام blur انجام دهید.
4. هنگام ذخیره، کنترل همان ردیف را loading و غیرفعال کنید تا درخواست تکراری ارسال نشود.
5. فقط در صورت `isSuccess === true` مقدار cache/state جدول را قطعی کنید؛ در خطا پیام `message` را نمایش دهید.
6. در صورت نیاز، `secretaryAnnouncementUpdatedAt` را به‌عنوان «آخرین تغییر» در tooltip یا متن کم‌رنگ نشان دهید.
7. برای ردیف‌های `isCanceled === true` ویرایش را غیرفعال کنید.

> این قابلیت مستقل از `secretaryReviewNote` است. `secretaryReviewNote` مربوط به بررسی حضور بعد از نوبت است؛ `secretaryAnnouncement` نتیجه تماس منشی پیش از نوبت را نگه می‌دارد.
