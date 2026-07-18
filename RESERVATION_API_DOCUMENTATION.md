# Reservation API Documentation

این سند APIهای سیستم رزرو را توضیح می‌دهد. برای این تغییر مایگریشن ساخته نشده است؛ قبل از اجرا باید جدول `Reservations` و ارتباط‌های آن را مطابق مدل `Reservation` در دیتابیس ایجاد کنید.

## قوانین اصلی

- رزرو فقط برای لیدهایی مجاز است که به همان مشاور اختصاص داده شده باشند.
- لید باید گزارش تماس ثبت‌شده داشته باشد و نتیجه تماس آن `Contacted = 1` یا `Converted = 2` باشد.
- مشاور در زمان ثبت رزرو دکتر انتخاب نمی‌کند.
- هر مشاور می‌تواند در یک زمان دقیق حداکثر برای ۱۰ بیمار رزرو فعال داشته باشد.
- برای هر لید فقط یک رزرو فعال قابل ثبت است.

## POST `/api/Reservation`

ثبت رزرو برای یک لید تماس موفق.

### Request body

```json
{
  "leadAssignmentId": 12,
  "consultantProfileId": 3,
  "reservationAt": "2026-06-20T10:30:00",
  "description": "توضیحات اختیاری"
}
```

### Success response

```json
{
  "isSuccess": true,
  "message": "رزرو با موفقیت ثبت شد",
  "data": {
    "id": 1,
    "leadAssignmentId": 12,
    "consultantProfileId": 3,
    "reservationAt": "2026-06-20T10:30:00",
    "patientName": "نام بیمار",
    "patientPhoneNumber": "09120000000"
  }
}
```

### Failure response examples

```json
{
  "isSuccess": false,
  "message": "فقط لیدهای تماس موفق قابل رزرو هستند",
  "data": null
}
```

```json
{
  "isSuccess": false,
  "message": "ظرفیت این بازه زمانی برای مشاور تکمیل است",
  "data": null
}
```

## GET `/api/Reservation/GetConsultantReservations`

لیست رزروهای ثبت‌شده برای یک مشاور و بیمارهای مربوطه.

### Query parameters

| Name | Type | Required | Description |
| --- | --- | --- | --- |
| `consultantProfileId` | long | yes | شناسه پروفایل مشاور |
| `from` | DateTime | no | شروع بازه زمانی |
| `to` | DateTime | no | پایان بازه زمانی |
| `includeCanceled` | bool | no | نمایش رزروهای کنسل‌شده؛ پیش‌فرض `false` |
| `pageNumber` | int | no | شماره صفحه؛ پیش‌فرض `1` |
| `pageSize` | int | no | اندازه صفحه؛ پیش‌فرض `10` |

### Example

```http
GET /api/Reservation/GetConsultantReservations?consultantProfileId=3&from=2026-06-20T00:00:00&to=2026-06-21T00:00:00&pageNumber=1&pageSize=10
```

### Response

```json
{
  "items": [
    {
      "id": 1,
      "leadAssignmentId": 12,
      "consultantProfileId": 3,
      "reservationAt": "2026-06-20T10:30:00",
      "patientName": "نام بیمار",
      "patientPhoneNumber": "09120000000",
      "description": "توضیحات اختیاری",
      "isCanceled": false
    }
  ],
  "totalCount": 1,
  "pageNumber": 1,
  "pageSize": 10,
  "totalPages": 1
}
```

## PUT `/api/Reservation`

ویرایش رزرو فعال توسط مشاور.

### Request body

```json
{
  "reservationId": 1,
  "consultantProfileId": 3,
  "reservationAt": "2026-06-20T11:00:00",
  "patientCity": "تهران",
  "patientRegion": "سعادت‌آباد",
  "attendanceProbabilityPercent": 80,
  "attendancePrediction": "بیمار گفت در تاریخ و ساعت رزرو شده در مطب حاضر می‌شود.",
  "secondaryPhoneNumber": "09121111111",
  "description": "توضیحات اختیاری"
}
```

### قوانین ویرایش

- رزرو باید متعلق به همان `consultantProfileId` باشد.
- رزروهای کنسل‌شده یا حذف‌شده قابل ویرایش نیستند.
- پس از بررسی منشی (`SecretaryApproved` / `SecretaryRejected`) امکان ویرایش وجود ندارد.
- اگر زمان رزرو تغییر کند، زمان جدید باید در آینده باشد.
- اگر زمان رزرو تغییر نکند، ویرایش سایر فیلدها حتی برای رزروهای گذشته مجاز است.
- شهر و منطقه بیمار الزامی است.
- `attendanceProbabilityPercent` باید بین ۰ تا ۱۰۰ باشد.

### Success response

```json
{
  "isSuccess": true,
  "message": "رزرو با موفقیت ویرایش شد",
  "data": {
    "id": 1,
    "leadAssignmentId": 12,
    "consultantProfileId": 3,
    "reservationAt": "2026-06-20T11:00:00",
    "patientName": "نام بیمار",
    "patientPhoneNumber": "09120000000",
    "patientCity": "تهران",
    "patientRegion": "سعادت‌آباد",
    "attendanceProbabilityPercent": 80,
    "attendancePrediction": "بیمار گفت در تاریخ و ساعت رزرو شده در مطب حاضر می‌شود.",
    "description": "توضیحات اختیاری",
    "isCanceled": false
  }
}
```

### Failure response examples

```json
{
  "isSuccess": false,
  "message": "پس از بررسی منشی امکان ویرایش رزرو وجود ندارد",
  "data": null
}
```

```json
{
  "isSuccess": false,
  "message": "زمان رزرو باید در آینده باشد",
  "data": null
}
```
