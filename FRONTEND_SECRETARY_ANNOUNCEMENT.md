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
  reservationId: number;
  appointmentDateTime: string; // زمان مراجعه بیمار
  createdAt: string;            // زمان ایجاد رکورد رزرو
  secretaryAnnouncement: string | null;
  secretaryAnnouncementStatus:
    | "NotCalled"
    | "NoAnswer"
    | "Confirmed"
    | "CancelledByPatient"
    | "RescheduleRequested"
    | "CallAgain"
    | null;
  secretaryAnnouncementUpdatedAt: string | null; // ISO DateTime, UTC
  secretaryAnnouncementUserId: string | null;    // Guid
  secretaryAnnouncementUserName: string | null;
};
```

نمونه آیتم:

```json
{
  "id": 124,
  "patientName": "علی رضایی",
  "reservationAt": "2026-08-20T10:30:00",
  "secretaryAnnouncement": "تماس گرفته شد؛ بیمار حضور فردا را تأیید کرد.",
  "secretaryAnnouncementStatus": "Confirmed",
  "secretaryAnnouncementUpdatedAt": "2026-08-19T08:15:00Z",
  "secretaryAnnouncementUserId": "4df08440-29bf-4ce0-953f-26686f043f04"
}
```

## قرارداد زمان رزرو

فیلد موجود `Reservation.ReservationAt` در بک‌اند همان **زمان مراجعه بیمار** و source of truth تمام داشبوردها است؛ بنابراین ستون جدیدی در دیتابیس و migration انتقال داده ایجاد نشده است. برای شفاف‌بودن قرارداد API، این مقدار در response با نام `appointmentDateTime` نیز برگردانده می‌شود و `createdAt` فقط زمان ساخته‌شدن رکورد است.

در ایجاد و ویرایش رزرو، فرانت‌اند جدید باید `appointmentDateTime` را به‌صورت ISO-8601 کامل ارسال کند:

```json
{
  "appointmentDateTime": "2026-08-21T17:00:00Z"
}
```

`reservationAt` فعلاً برای سازگاری با کلاینت قدیمی قابل ارسال است. اگر هر دو فیلد ارسال شوند باید دقیقاً برابر باشند، وگرنه درخواست رد می‌شود. مقدار ناقصی مانند `3` یک DateTime معتبر نیست و به‌دلیل `[ApiController]` با خطای HTTP 400 رد می‌شود.

لیست ادمین/منشی (`GET /api/reservations` و endpoint قدیمی SecretaryReservations)، لیست مشاور و گزارش روزانه ادمین همگی زمان مراجعه را مستقیماً از همان `Reservation.ReservationAt` می‌خوانند. مرتب‌سازی و فیلتر گزارش روزانه نیز بر اساس زمان مراجعه انجام می‌شود، نه `createdAt`.

## ثبت یا ویرایش اعلام منشی

```http
PUT /api/Reservation/SecretaryAnnouncement
Content-Type: application/json
```

```json
{
  "reservationId": 124,
  "status": "Confirmed",
  "description": "تماس گرفته شد؛ بیمار حضور فردا را تأیید کرد."
}
```

- `reservationId` شناسه رزرو همان ردیف است.
- شناسه منشی از JWT کاربر لاگین‌شده خوانده می‌شود و نباید در body ارسال شود.
- `status` یکی از شش وضعیت قرارداد بالا و اجباری است.
- `description` متن آزاد با حداکثر **۱۰۰۰ کاراکتر** است.
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

## فیلتر و گزارش

مسیر یکپارچه لیست و فیلتر:

```http
GET /api/reservations?search=0912&fromDate=2026-08-18&toDate=2026-08-25&consultantId=12&secretaryAnnouncementStatus=NoAnswer&reservationStatus=PendingConsultantConfirmation
```

`search` روی نام بیمار، شماره اصلی و شماره دوم جستجو می‌کند. همه پارامترها اختیاری هستند. شناسه رزرو و مشاور در این سامانه از نوع `long` است.

خلاصه داشبورد:

```http
GET /api/secretary/dashboard/summary
```

```json
{ "needCall": 20, "confirmed": 30, "noAnswer": 5, "cancelled": 3 }
```

`needCall` شامل رزروهای فعال با وضعیت `null` یا `NotCalled` است. رزروهای حذف‌شده و لغوشده در آمار محاسبه نمی‌شوند.

## قرارداد Web Push مشاور

برای وضعیت‌های `NoAnswer`، `Confirmed` و `CancelledByPatient` یک Web Push به کاربر مشاور مرتبط ارسال می‌شود. envelope استاندارد Push برنامه به شکل زیر است و قرارداد پایدار رویداد داخل `data` قرار دارد:

```json
{
  "title": "اعلام منشی رزرو",
  "body": "بیمار رزرو خود را تایید کرد.",
  "data": {
    "type": "ReservationSecretaryConfirmed",
    "reservationId": "124",
    "patientName": "علی احمدی",
    "reservationDate": "2026-08-20",
    "message": "بیمار رزرو خود را تایید کرد."
  }
}
```

مقادیر پایدار `data.type`:

- `ReservationSecretaryNoAnswer`
- `ReservationSecretaryConfirmed`
- `ReservationSecretaryCancelled`

ذخیره نتیجه تماس وابسته به تحویل Push نیست؛ در صورت نداشتن subscription فعال، نتیجه تماس همچنان با موفقیت ذخیره می‌شود.
