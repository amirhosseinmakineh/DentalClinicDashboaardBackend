# مستند فرانت‌اند: لیست رزروها و تایید حضور بیمار (مشاور)

> **مستند کامل پیاده‌سازی:** [`frontend-reservation-attendance-implementation-fa.md`](./frontend-reservation-attendance-implementation-fa.md)

این فایل خلاصه است. برای جزئیات صفحات، TypeScript types، منطق دکمه‌ها، سناریوهای بیزینسی و نمونه کد به مستند اصلی مراجعه کنید.

## APIهای کلیدی مشاور

| API | کاربرد |
|---|---|
| `GET /api/Reservation/GetConsultantReservations` | لیست همه / انجام‌شده |
| `GET /api/Reservation/DueConfirmations` | رزروهای آماده تایید حضور |
| `POST /api/Reservation/ConfirmAttendance` | ثبت «بیمار آمد / نیامد» |
| `POST /api/Reservation` | ثبت رزرو جدید |

## تب‌های پیشنهادی

1. **در انتظار تایید** → `DueConfirmations`
2. **همه** → `GetConsultantReservations`
3. **انجام‌شده** → `GetConsultantReservations?onlySecretaryReviewed=true`
