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
