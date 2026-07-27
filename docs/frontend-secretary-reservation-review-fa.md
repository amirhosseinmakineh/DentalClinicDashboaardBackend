# مستند فرانت‌اند: تایید یا تغییر زمان رزرو توسط منشی

## هدف و جریان کسب‌وکار

هر رزرو جدید با وضعیت بررسی منشی `Pending` ساخته می‌شود. منشی در داشبورد خود یکی از این دو عمل را **فقط یک بار** انجام می‌دهد:

1. **تایید زمان فعلی:** درخواست بررسی بدون `newReservationAt` ارسال می‌شود و وضعیت به `Approved` می‌رود.
2. **تغییر و نهایی‌کردن زمان:** درخواست با `newReservationAt` متفاوت ارسال می‌شود؛ زمان رزرو تغییر می‌کند و وضعیت به `Rescheduled` می‌رود.

هر دو وضعیت `Approved` و `Rescheduled` نهایی هستند. پس از نهایی‌شدن، مشاور نیز دیگر نمی‌تواند رزرو را ویرایش کند. این فرایند مستقل از فرایند «تایید حضور بیمار پس از زمان رزرو» و فیلدهای `attendanceConfirmationStatus` است.

## مقادیر وضعیت بررسی رزرو

`SecretaryReservationReviewStatus` در JSON به‌صورت عدد برمی‌گردد:

| مقدار | نام | کاربرد در رابط کاربری |
|---:|---|---|
| `1` | `Pending` | نمایش دکمه‌های «تایید» و «تغییر زمان» |
| `2` | `Approved` | زمان فعلی توسط منشی تایید شده؛ عملیات غیرفعال شود |
| `3` | `Rescheduled` | زمان توسط منشی تغییر و نهایی شده؛ عملیات غیرفعال شود |

## دریافت رزروهای داشبورد منشی

### `GET /api/Reservation/SecretaryReservations`

پارامترهای جدید query:

| پارامتر | نوع | اجباری | توضیح |
|---|---|---:|---|
| `reservationReviewStatus` | integer | خیر | فیلتر وضعیت: `1`، `2` یا `3` |
| `onlyPendingReservationReview` | boolean | خیر | اگر `true` باشد فقط رزروهای منتظر بررسی را برمی‌گرداند |

پارامترهای قبلی مانند `consultantProfileId`، `date`، `from`، `to`، `includeCanceled`، `pageNumber` و `pageSize` بدون تغییر هستند.

نمونه درخواست صف بررسی:

```http
GET /api/Reservation/SecretaryReservations?onlyPendingReservationReview=true&pageNumber=1&pageSize=20
```

نمونه خروجی (فیلدهای مرتبط):

```json
{
  "items": [
    {
      "id": 42,
      "consultantProfileId": 7,
      "consultantFullName": "علی رضایی",
      "patientName": "مریم احمدی",
      "patientPhoneNumber": "09121234567",
      "reservationAt": "2026-07-29T10:30:00",
      "secretaryReservationReviewStatus": 1,
      "secretaryReservationReviewedAt": null,
      "secretaryReservationReviewerUserId": null,
      "secretaryReservationReviewNote": null,
      "attendanceConfirmationStatus": 1,
      "isCanceled": false
    }
  ],
  "totalCount": 1,
  "pageNumber": 1,
  "pageSize": 20,
  "totalPages": 1,
  "hasPrevious": false,
  "hasNext": false
}
```

## تایید زمان فعلی رزرو

### `POST /api/Reservation/ReviewSecretaryReservation`

ورودی:

```json
{
  "reservationId": 42,
  "secretaryUserId": "b8cc659f-c550-4c7f-9e8d-a04ee41cb555",
  "note": "زمان با بیمار هماهنگ شد"
}
```

`newReservationAt` ارسال نشود یا `null` باشد. خروجی موفق:

```json
{
  "isSuccess": true,
  "message": "رزرو با موفقیت توسط منشی تایید شد",
  "data": {
    "reservationId": 42,
    "reservationAt": "2026-07-29T10:30:00",
    "reviewStatus": 2,
    "reviewedAt": "2026-07-27T12:10:00Z",
    "secretaryUserId": "b8cc659f-c550-4c7f-9e8d-a04ee41cb555",
    "note": "زمان با بیمار هماهنگ شد"
  }
}
```

## تغییر و نهایی‌کردن زمان رزرو

### `POST /api/Reservation/ReviewSecretaryReservation`

ورودی:

```json
{
  "reservationId": 42,
  "secretaryUserId": "b8cc659f-c550-4c7f-9e8d-a04ee41cb555",
  "newReservationAt": "2026-07-29T11:30:00",
  "note": "زمان جدید با بیمار هماهنگ شد"
}
```

خروجی موفق همان ساختار بالاست، با `reviewStatus: 3` و مقدار جدید در `reservationAt`.

## قواعد اعتبارسنجی و رفتار پیشنهادی فرانت

- عملیات فقط برای رزرو فعال، حذف‌نشده و کنسل‌نشده مجاز است.
- رزرو فقط در وضعیت `Pending` قابل بررسی است؛ درخواست تکراری شکست می‌خورد.
- زمان نهایی باید در آینده باشد.
- در تغییر زمان، سقف فعلی سیستم رعایت می‌شود: حداکثر ۱۰ رزرو فعال برای همان مشاور در یک زمان دقیق.
- `secretaryUserId` نباید Guid خالی باشد و باید از شناسه کاربر منشی لاگین‌شده ارسال شود.
- `note` اختیاری و حداکثر ۱۰۰۰ کاراکتر است.
- موفق یا ناموفق بودن HTTP در معماری فعلی با بدنه مشخص می‌شود؛ فرانت حتما `isSuccess` را بررسی کند و در شکست، متن `message` را نمایش دهد.
- هنگام submit هر دو دکمه غیرفعال شوند. بعد از موفقیت، آیتم از صف Pending حذف یا لیست دوباره دریافت شود.
- برای تغییر زمان، مقدار `newReservationAt` را با همان قرارداد تاریخ فعلی سیستم (ISO-8601) ارسال کنید. زمان برابر با زمان قبلی به‌عنوان «تایید» ثبت می‌شود، نه «تغییر زمان».

نمونه شکست:

```json
{
  "isSuccess": false,
  "message": "این رزرو قبلا توسط منشی بررسی شده است",
  "data": null
}
```

پیام‌های شکست دیگر می‌توانند شامل «رزرو فعال یافت نشد»، «شناسه منشی الزامی است»، «زمان رزرو باید در آینده باشد»، «ظرفیت این بازه زمانی برای مشاور تکمیل است» و خطای طول توضیحات باشند.
