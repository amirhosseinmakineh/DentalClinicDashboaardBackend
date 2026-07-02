# مستند فرانت‌اند: داشبورد منشی — رزرو، تایید حضور و امتیاز مشاور

> **مستند کامل پیاده‌سازی:** [`frontend-reservation-attendance-implementation-fa.md`](./frontend-reservation-attendance-implementation-fa.md)

این فایل خلاصه است. برای جزئیات صفحات، TypeScript types، منطق دکمه‌ها، سناریوهای بیزینسی و نمونه کد به مستند اصلی مراجعه کنید.

## APIهای کلیدی منشی

| API | کاربرد |
|---|---|
| `GET /api/Reservation/SecretaryReservations` | لیست رزرو همه مشاورها + جستجو/فیلتر |
| `POST /api/Reservation/CompletePatientProfile` | تکمیل پرونده بیمار |
| `POST /api/Reservation/ReviewAttendance` | تایید/رد اظهار مشاور + امتیاز ±۱۰ |
| `GET /api/Consultant/GetConsultants` | dropdown انتخاب مشاور |

## تب‌های پیشنهادی

1. **صف بررسی** → `onlyWaitingForSecretaryReview=true&onlyDue=true`
2. **همه رزروها** → با `searchText` و فیلترها
3. **انجام‌شده** → `attendanceConfirmationStatus=4` یا `5`
