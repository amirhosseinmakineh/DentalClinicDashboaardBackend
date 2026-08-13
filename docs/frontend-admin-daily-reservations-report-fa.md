# راهنمای فرانت گزارش روزانه رزروهای ادمین

## هدف گزارش

این گزارش، **رزروهایی را نشان می‌دهد که در یک روز کاری توسط مشاوران ثبت شده‌اند**؛ بنابراین فیلتر روز روی `createdAt` است، نه روی زمان مراجعه (`reservationAt`). مرز روز بر اساس منطقه زمانی ایران (`Asia/Tehran`) محاسبه می‌شود. اگر `date` ارسال نشود، امروز ایران در نظر گرفته می‌شود.

تمام endpointهای این صفحه فقط با توکن کاربر دارای نقش `Admin` در دسترس‌اند:

```http
Authorization: Bearer <admin-access-token>
```

## ۱. دریافت گزارش برای نمایش در پنل

```http
GET /api/admin/reports/daily-reservations?date=2026-08-12&consultantProfileId=15&requestStatus=2
```

### Query parameterها

| پارامتر | نوع | اجباری | توضیح |
|---|---|---:|---|
| `date` | `YYYY-MM-DD` | خیر | روز ثبت رزرو به وقت ایران؛ پیش‌فرض امروز ایران |
| `consultantProfileId` | number | خیر | محدود کردن گزارش به یک مشاور |
| `requestStatus` | number | خیر | محدود کردن گزارش به وضعیت درخواست رزرو |

مقادیر `requestStatus`:

| مقدار | عنوان |
|---:|---|
| 1 | در انتظار بررسی منشی |
| 2 | تایید شده |
| 3 | زمان‌بندی مجدد |
| 4 | رد شده |
| 5 | لغو شده |
| 6 | در انتظار تایید بیمار |
| 7 | نیازمند پیگیری |

### نمونه پاسخ

```json
{
  "date": "2026-08-12",
  "generatedAt": "2026-08-12T10:30:00Z",
  "summary": {
    "total": 12,
    "active": 10,
    "canceled": 2,
    "pendingSecretaryReview": 3,
    "confirmed": 5,
    "rescheduled": 1,
    "rejected": 1,
    "uniqueConsultants": 4
  },
  "items": [
    {
      "reservationId": 1201,
      "leadAssignmentId": 802,
      "consultantProfileId": 15,
      "consultantFullName": "علی احمدی",
      "consultantPhoneNumber": "09120000000",
      "patientName": "مریم محمدی",
      "patientPhoneNumber": "09121111111",
      "secondaryPhoneNumber": null,
      "patientCity": "تهران",
      "patientRegion": "ونک",
      "businessName": null,
      "attendanceProbabilityPercent": 80,
      "reservationAt": "2026-08-14T07:30:00Z",
      "createdAt": "2026-08-12T08:10:00Z",
      "requestStatus": 2,
      "requestStatusTitle": "تایید شده",
      "visitResultStatus": 1,
      "visitResultStatusTitle": "در انتظار مراجعه",
      "isConfirmedWithPatient": true,
      "isCanceled": false,
      "cancellationReason": null,
      "description": "نیاز به مشاوره ایمپلنت"
    }
  ]
}
```

تاریخ‌وساعت‌های JSON با UTC برمی‌گردند. فرانت برای نمایش باید آن‌ها را به وقت ایران تبدیل کند. برای badgeها از فیلدهای آماده `requestStatusTitle` و `visitResultStatusTitle` استفاده کنید و به ترجمه enum در فرانت وابسته نشوید.

## ۲. دانلود فایل اکسل (CSV سازگار با Excel)

```http
GET /api/admin/reports/daily-reservations/export?date=2026-08-12&consultantProfileId=15&requestStatus=2
```

فیلترهای این endpoint دقیقاً مشابه endpoint نمایش هستند. پاسخ یک فایل CSV با BOM از نوع UTF-8 است تا متن فارسی مستقیماً در Microsoft Excel درست باز شود. تاریخ‌های داخل فایل از قبل به وقت ایران تبدیل شده‌اند.

نام فایل به شکل زیر است:

```text
daily-reservations-20260812.csv
```

### نمونه پیاده‌سازی دانلود

```ts
async function downloadDailyReservations(filters: {
  date?: string;
  consultantProfileId?: number;
  requestStatus?: number;
}) {
  const params = new URLSearchParams();
  if (filters.date) params.set('date', filters.date);
  if (filters.consultantProfileId)
    params.set('consultantProfileId', String(filters.consultantProfileId));
  if (filters.requestStatus)
    params.set('requestStatus', String(filters.requestStatus));

  const response = await fetch(
    `/api/admin/reports/daily-reservations/export?${params.toString()}`,
    { headers: { Authorization: `Bearer ${accessToken}` } },
  );

  if (!response.ok) throw new Error('دریافت گزارش ناموفق بود');
  const blob = await response.blob();
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = `daily-reservations-${filters.date ?? 'today'}.csv`;
  link.click();
  URL.revokeObjectURL(url);
}
```

## پیشنهاد UI

1. مقدار اولیه فیلتر تاریخ را امروز ایران قرار دهید.
2. کارت‌های خلاصه را از `summary` بسازید.
3. جدول جزئیات را از `items` نمایش دهید و مرتب‌سازی پیش‌فرض را تغییر ندهید؛ API جدیدترین رزرو ثبت‌شده را اول می‌فرستد.
4. فیلتر مشاور و وضعیت را هم‌زمان به API نمایش و API خروجی ارسال کنید تا فایل دقیقاً مطابق جدول باشد.
5. هنگام دانلود، spinner مستقل روی دکمه «خروجی اکسل» نشان دهید و پاسخ را به شکل JSON parse نکنید.
6. وضعیت `401` را به ورود مجدد و `403` را به صفحه عدم دسترسی هدایت کنید.

> رزروهای لغوشده نیز عمداً در گزارش کامل روزانه وجود دارند و در `summary.canceled` و ستون «لغو شده» مشخص می‌شوند. رکوردهای soft-delete شده در هیچ‌کدام از خروجی‌ها نمایش داده نمی‌شوند.
