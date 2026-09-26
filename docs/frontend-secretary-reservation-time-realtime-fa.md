# راهنمای فرانت‌اند: تغییر زمان رزرو توسط منشی و همگام‌سازی زنده

## تغییر زمان از داشبورد منشی

منشی دارای دسترسی `EditReservations` می‌تواند زمان یک رزرو در محدوده دسترسی خود را تغییر دهد:

```http
PATCH /api/Reservation/SecretaryReservations/{reservationId}/time
Authorization: Bearer <access-token>
Content-Type: application/json

{
  "reservationAt": "2026-08-25T11:30:00Z"
}
```

`appointmentDateTime` نیز به‌عنوان نام جایگزین پذیرفته می‌شود. اگر هر دو فیلد ارسال شوند باید برابر باشند. بک‌اند شناسه مشاور را از خود رزرو می‌خواند؛ بنابراین فرانت‌اند نباید `consultantProfileId` را ارسال کند.

همان محدودیت‌های ویرایش رزرو اعمال می‌شوند: رزرو باید فعال باشد، زمان جدید باید در آینده باشد، ظرفیت آن بازه تکمیل نباشد و رزرو قبلاً توسط منشی تأیید یا رد نشده باشد. پاسخ موفق در `data` شامل نسخه کامل و به‌روز رزرو است.

## دریافت تغییر در همه داشبوردها

همه داشبوردهای لاگین‌شده به هاب زیر متصل شوند:

```text
/hubs/reservations
```

توکن JWT در SignalR از طریق `accessTokenFactory` ارسال شود. سرور بعد از ویرایش موفق منشی، مشاور یا endpoint عمومی ویرایش، رویداد `ReservationUpdated` را برای تمام اتصال‌ها منتشر می‌کند:

```json
{
  "reservationId": 125,
  "consultantProfileId": 3,
  "reservationAt": "2026-08-25T11:30:00Z",
  "appointmentDateTime": "2026-08-25T11:30:00Z",
  "updatedByUserId": "00000000-0000-0000-0000-000000000000",
  "updatedAt": "2026-08-19T12:00:00Z",
  "reservation": {}
}
```

نمونه اتصال:

```ts
const connection = new signalR.HubConnectionBuilder()
  .withUrl(`${apiBaseUrl}/hubs/reservations`, {
    accessTokenFactory: () => accessToken,
  })
  .withAutomaticReconnect()
  .build();

connection.on("ReservationUpdated", event => {
  // در صورت وجود آیتم، آن را با event.reservation جایگزین کنید.
  // اگر فیلتر تاریخ باعث خروج یا ورود آیتم می‌شود، لیست را دوباره واکشی کنید.
  refreshReservations();
});

await connection.start();
```

رویداد به همه نقش‌ها ارسال می‌شود تا داشبورد منشی، مشاور و مدیر هم‌زمان اطلاعات یکسانی نمایش دهند. اتصال هاب نیازمند احراز هویت است.
