# مستند تغییرات فرانت: گزارش تماس لیدها و رزرو

## ثبت گزارش تماس لید

`POST /api/Consultant/SubmitLeadCallReport`

فیلدهای جدیدی که باید در فرم گزارش تماس ارسال شوند:

```json
{
  "leadAssignmentId": 1,
  "consultantProfileId": 10,
  "callResult": 1,
  "reportDescription": "متن گزارش مشاور",
  "patientCity": "تهران",
  "patientRegion": "سعادت‌آباد",
  "businessName": "کلینیک/بیزینس نمونه",
  "attendanceProbabilityPercent": 80
}
```

- `attendanceProbabilityPercent` اختیاری است، اما اگر ارسال شود باید بین 0 تا 100 باشد.
- `patientCity`، `patientRegion`، `businessName` از رزرو به گزارش تماس لید منتقل شده‌اند.

## ثبت رزرو

`POST /api/Reservation`

فیلدهای شهر، منطقه، بیزینس و احتمال حضور دیگر از فرم رزرو ارسال نشوند. فیلد جدید رزرو:

```json
{
  "leadAssignmentId": 1,
  "consultantProfileId": 10,
  "reservationAt": "2026-06-29T12:00:00",
  "secondaryPhoneNumber": "09120000000",
  "description": "توضیح رزرو"
}
```

## خروجی اکسل/CSV ادمین

`GET /api/admin/reports/lead-call-reports/export?from=2026-06-01&to=2026-06-28`

- خروجی فایل CSV با BOM است و در Excel باز می‌شود.
- اگر تاریخ ارسال نشود، خروجی گزارش‌های روز جاری برگردانده می‌شود.
- ستون‌ها شامل اطلاعات لید، نام و شماره مشاور، نتیجه تماس، متن گزارش مشاور، شهر، منطقه، بیزینس، احتمال حضور و وضعیت لید هستند.

## نحوه گزارش‌گیری

گزارش تماس لیدها فقط به‌صورت دستی و با درخواست ادمین تولید می‌شود. فرانت باید هنگام کلیک ادمین روی دکمه گزارش‌گیری، endpoint خروجی CSV را صدا بزند و فایل برگشتی را دانلود کند.

هیچ سرویس بک‌گراندی برای تولید خودکار گزارش روزانه اجرا نمی‌شود و فایلی به‌صورت زمان‌بندی‌شده روی سرور ذخیره نمی‌گردد.
