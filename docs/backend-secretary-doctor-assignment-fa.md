# قرارداد بک‌اند — تخصیص دکتر به بیمار توسط منشی

این سند قرارداد API صفحه «تخصیص دکتر به بیمار» در پنل منشی را مشخص می‌کند.

## ۱. دریافت رزروها در بازه تاریخ

از Endpoint موجود رزروهای منشی استفاده می‌شود:

```http
GET /api/Reservation/SecretaryReservations
```

| پارامتر | نوع | الزامی | توضیح |
| --- | --- | --- | --- |
| `fromDate` | `YYYY-MM-DD` | بله | ابتدای بازه تاریخ رزرو (inclusive) |
| `toDate` | `YYYY-MM-DD` | بله | انتهای بازه تاریخ رزرو (inclusive) |
| `includeCanceled` | `boolean` | بله | برای این صفحه `false` |
| `sortDirection` | `asc` یا `desc` | بله | برای این صفحه `asc` |
| `pageNumber` | `number` | بله | از `1` شروع می‌شود |
| `pageSize` | `number` | بله | حداکثر `100` |

بازه تاریخ بر مبنای منطقه زمانی ایران اعمال می‌شود. ساختار صفحه‌بندی همان قرارداد فعلی باقی می‌ماند و هر آیتم، علاوه بر اطلاعات فعلی، `doctorName` را نیز برمی‌گرداند.

## ۲. دریافت جزئیات رزرو

```http
GET /api/Reservation/{reservationId}
Authorization: Bearer <token>
```

### پاسخ موفق

```json
{
  "id": 125,
  "patientName": "علی رضایی",
  "patientPhoneNumber": "09121234567",
  "consultantFullName": "مریم احمدی",
  "reservationAt": "2026-08-25T09:30:00",
  "doctorName": "دکتر محمدی"
}
```

رزرو فقط در صورتی برگردانده می‌شود که منشی جاری مجوز مشاهده آن را داشته باشد.

## ۳. ثبت نام دکتر

```http
POST /api/Reservation/{reservationId}/assign-doctor
Content-Type: application/json
Authorization: Bearer <token>
```

```json
{
  "doctorName": "دکتر محمدی"
}
```

### قواعد اعتبارسنجی

- `doctorName` بعد از Trim نباید خالی باشد.
- حداکثر طول نام ۱۵۰ نویسه است.
- شناسه منشی از JWT خوانده می‌شود.
- منشی باید مجوز و دسترسی به رزرو موردنظر را داشته باشد.
- رزرو حذف‌شده قابل تغییر نیست.
- ثبت مجدد، مقدار قبلی `doctorName` را جایگزین می‌کند.

پاسخ موفق از Envelope استاندارد پروژه استفاده می‌کند و پیام «دکتر با موفقیت به بیمار تخصیص داده شد.» را برمی‌گرداند. خطاها با Status Code مناسب (`400`، `401`، `403` یا `404`) برگردانده می‌شوند.
