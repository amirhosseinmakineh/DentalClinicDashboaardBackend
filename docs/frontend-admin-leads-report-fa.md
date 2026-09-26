# اکویومنت فرانت گزارش لیدهای سیستم

گزارش لیدها در دو خروجی **نمایش وب (JSON)** و **دانلود CSV سازگار با Excel** در
اختیار ادمین است. هر دو endpoint دقیقاً از فیلترهای یکسان استفاده می‌کنند و فقط
کاربر دارای نقش `Admin` مجاز به فراخوانی آن‌ها است.

## ۱. نمایش گزارش در وب

```http
GET /api/admin/reports/leads?pageNumber=1&pageSize=25
Authorization: Bearer <admin-token>
```

### فیلترها

| پارامتر | نوع | توضیح |
|---|---|---|
| `from` | `YYYY-MM-DD` | تاریخ ایجاد لید از این روز (شامل ابتدا) |
| `to` | `YYYY-MM-DD` | تاریخ ایجاد لید تا پایان این روز (شامل انتها) |
| `consultantProfileId` | `long` | شناسه پروفایل مشاور |
| `leadAssignmentState` | `number` | وضعیت لید طبق جدول پایین |
| `assignmentType` | `number` | نوع تخصیص طبق جدول پایین |
| `callResult` | `number` | نتیجه تماس طبق جدول پایین |
| `isAssigned` | `boolean` | فقط لیدهای اساین‌شده/نشده |
| `hasCalled` | `boolean` | فقط لیدهای تماس‌گرفته/نگرفته |
| `hasSubmittedReport` | `boolean` | فقط لیدهای دارای/فاقد گزارش مشاور |
| `searchText` | `string` | جستجو در نام، موبایل، شماره دوم و نام بیزینس |
| `pageNumber` | `number` | پیش‌فرض `1` |
| `pageSize` | `number` | پیش‌فرض `25` و حداکثر `200` |

فیلترها اختیاری‌اند و ترکیب آن‌ها با منطق **AND** انجام می‌شود. اگر `from` بعد از
`to` باشد پاسخ `400` برمی‌گردد. فاصله ابتدا و انتهای `searchText` حذف می‌شود.

### enumها

- `leadAssignmentState`: `1=New`، `2=Assigned`، `3=Contacted`، `4=Pending`،
  `5=Converted`، `6=Expired`، `7=Rejected`
- `assignmentType`: `1=RealTime`، `3=ConsultantPatient`
- `callResult`: `1=Contacted`، `2=Converted`، `3=Rejected`، `4=NoAnswer`،
  `5=WrongNumber`، `6=NeedFollowUp`، `7=Busy`، `8=PatientHungUp`

### نمونه درخواست کامل

```http
GET /api/admin/reports/leads?from=2026-08-01&to=2026-08-21&consultantProfileId=12&assignmentType=1&hasCalled=true&hasSubmittedReport=true&searchText=0912&pageNumber=1&pageSize=25
```

### ساختار پاسخ

```json
{
  "summary": {
    "total": 42,
    "assigned": 38,
    "unassigned": 4,
    "called": 30,
    "notCalled": 12,
    "converted": 8,
    "withSubmittedReport": 29
  },
  "items": [
    {
      "leadId": 1001,
      "leadName": "علی رضایی",
      "leadPhoneNumber": "09120000000",
      "secondaryPhoneNumber": null,
      "leadAssignmentState": 5,
      "leadAssignmentStateTitle": "تبدیل شده",
      "assignmentType": 1,
      "assignmentTypeTitle": "آنی",
      "consultantProfileId": 12,
      "consultantFullName": "مشاور نمونه",
      "consultantPhoneNumber": "09121111111",
      "isAssigned": true,
      "hasCalled": true,
      "callResult": 2,
      "callResultTitle": "تبدیل به رزرو",
      "reportDescription": "رزرو انجام شد",
      "assignedAt": "2026-08-21T08:10:00",
      "assignedAtPersian": "1405/05/30 08:10",
      "contactedAt": "2026-08-21T08:13:00",
      "contactedAtPersian": "1405/05/30 08:13",
      "reportSubmittedAt": "2026-08-21T08:15:00",
      "reportSubmittedAtPersian": "1405/05/30 08:15",
      "createdAt": "2026-08-21T08:00:00",
      "createdAtPersian": "1405/05/30 08:00",
      "patientCity": "تهران",
      "patientRegion": "سعادت‌آباد",
      "businessName": "کلینیک نمونه",
      "attendanceProbabilityPercent": 80
    }
  ],
  "pageNumber": 1,
  "pageSize": 25,
  "totalCount": 42,
  "totalPages": 2
}
```

اعداد بخش `summary` روی **تمام نتیجه فیلترشده** محاسبه می‌شوند، نه فقط صفحه جاری.

## ۲. دانلود اکسل (CSV)

```http
GET /api/admin/reports/leads/export?from=2026-08-01&to=2026-08-21&hasCalled=true
Authorization: Bearer <admin-token>
```

همه فیلترهای endpoint وب قابل ارسال‌اند. `pageNumber` و `pageSize` در خروجی فایل
نادیده گرفته می‌شوند و **تمام رکوردهای منطبق** دانلود می‌شوند. پاسخ با
`Content-Type: text/csv; charset=utf-8` و BOM است و متن فارسی در Excel درست نمایش
داده می‌شود.

در Angular برای دریافت فایل از `responseType: 'blob'` استفاده کنید:

```ts
downloadLeadsReport(params: Record<string, string | number | boolean>) {
  return this.http.get('/api/admin/reports/leads/export', {
    params,
    responseType: 'blob'
  });
}
```

برای اینکه فایل و جدول دقیقاً یک خروجی داشته باشند، همان آبجکت فیلتر جدول را
بدون `pageNumber` و `pageSize` به endpoint دانلود ارسال کنید.
