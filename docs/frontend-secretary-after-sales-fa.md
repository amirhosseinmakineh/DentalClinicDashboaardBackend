# راهنمای تغییرات فرانت‌اند داشبورد منشی: مراجعه و خدمات پس از فروش

این سند قرارداد فرانت‌اند تغییرات جدید داشبورد منشی را توضیح می‌دهد. تغییرات شامل ثبت نتیجه واقعی مراجعه، مرتب‌سازی وقت‌ها، بخش «خدمات پس از فروش» و نمایش تاریخ‌های شمسی است.

## خلاصه کارهای لازم در فرانت

1. در نتیجه مراجعه به‌جای مفهوم مبهم «تأیید مشاور»، از منشی بپرسید: **«آیا برای بیمار خدمت انجام شد؟»**
2. لیست رزروها را با ترتیب دریافتی از API نمایش دهید؛ ترتیب پیش‌فرض بر اساس نزدیک‌ترین `reservationAt` است.
3. یک تب یا بخش جدید با عنوان **«خدمات پس از فروش»** ایجاد کنید.
4. هنگام ایجاد وقت خدمات پس از فروش، مقدار `reservationType: 2` ارسال شود.
5. برای نمایش تاریخ‌ها از فیلدهای `...Persian` استفاده شود؛ تاریخ ISO فقط برای محاسبات، ارسال فرم و date picker نگهداری شود.

---

## enum نوع رزرو

| مقدار | نام | عنوان نمایشی |
|---:|---|---|
| `1` | `Regular` | رزرو عادی |
| `2` | `AfterSalesService` | خدمات پس از فروش |

در requestها مقدار عددی ارسال شود تا وابستگی فرانت به تنظیمات serialize شدن enum در بک‌اند ایجاد نشود.

```ts
export enum ReservationType {
  Regular = 1,
  AfterSalesService = 2,
}
```

---

## ۱. بخش خدمات پس از فروش

### پیشنهاد UI

در داشبورد منشی کنار رزروهای عادی یک تب جدید اضافه کنید:

- `همه رزروها`
- `رزروهای عادی`
- `خدمات پس از فروش`

تعداد کل رزروهای لغونشده خدمات پس از فروش در خلاصه داشبورد برگردانده می‌شود:

```http
GET /api/Secretary/dashboard/summary
Authorization: Bearer <secretary-token>
```

نمونه پاسخ:

```json
{
  "needCall": 8,
  "confirmed": 12,
  "noAnswer": 2,
  "cancelled": 1,
  "afterSalesServices": 4
}
```

کارت «خدمات پس از فروش» مقدار `afterSalesServices` را نمایش دهد و با کلیک روی آن، لیست با فیلتر `reservationType=2` باز شود.

### دریافت لیست خدمات پس از فروش

```http
GET /api/Reservation/SecretaryReservations?reservationType=2&pageNumber=1&pageSize=20
Authorization: Bearer <secretary-token>
```

مسیر زیر نیز همین قرارداد را دارد:

```http
GET /api/reservations?reservationType=2&pageNumber=1&pageSize=20
```

برای رزروهای عادی از `reservationType=1` استفاده کنید. برای تب «همه» پارامتر `reservationType` را ارسال نکنید.

### ایجاد وقت خدمات پس از فروش

```http
POST /api/Reservation
Authorization: Bearer <secretary-token>
Content-Type: application/json
```

```json
{
  "leadAssignmentId": 802,
  "consultantProfileId": 15,
  "reservationAt": "2026-08-25T10:30:00",
  "description": "بررسی نتیجه درمان و خدمات پس از فروش",
  "reservationType": 2
}
```

نکات پیاده‌سازی:

- همان فرم فعلی ایجاد رزرو قابل استفاده است؛ فقط عنوان فرم و `reservationType` تغییر می‌کند.
- انتخاب بیمار/لید و مشاور همچنان اجباری است.
- `reservationAt` باید در آینده باشد.
- فرانت تاریخ و ساعت انتخابی را با قرارداد فعلی پروژه به‌صورت ISO ارسال کند؛ مقدار شمسی برای نمایش است، نه payload ذخیره‌سازی.
- اگر بیمار رزرو باز داشته باشد API خطا می‌دهد. پس از نهایی شدن رزرو قبلی با تأیید یا رد منشی، امکان ثبت وقت بعدی از جمله خدمات پس از فروش وجود دارد.
- ایجاد رزرو برای منشی نیازمند permission با نام `CreateReservation` است. در خطای `403` دکمه یا فرم ایجاد رزرو را غیرفعال کنید.

نمونه بخش مهم پاسخ ایجاد رزرو:

```json
{
  "isSuccess": true,
  "data": {
    "reservationId": 1202,
    "leadAssignmentId": 802,
    "consultantProfileId": 15,
    "reservationAt": "2026-08-25T10:30:00",
    "appointmentDateTime": "2026-08-25T10:30:00",
    "reservationType": 2,
    "attendanceConfirmationStatus": 1
  },
  "message": "رزرو با موفقیت ثبت شد"
}
```

---

## ۲. ثبت نتیجه مراجعه توسط منشی

```http
POST /api/Reservation/ReviewAttendance
Authorization: Bearer <secretary-token>
Content-Type: application/json
```

### خدمت برای بیمار انجام شده است

```json
{
  "reservationId": 1202,
  "patientReceivedService": true,
  "note": "خدمت موردنظر کامل انجام شد"
}
```

نتیجه رزرو به `SecretaryApproved` با مقدار عددی `4` تغییر می‌کند.

### بیمار آمده ولی خدمت انجام نشده است

```json
{
  "reservationId": 1202,
  "patientReceivedService": false,
  "note": "بیمار مراجعه کرد اما خدمت انجام نشد"
}
```

نتیجه رزرو به `SecretaryRejected` با مقدار عددی `5` تغییر می‌کند.

### قواعد UI

- کنترل پیشنهادی دو دکمه یا radio است:
  - **خدمت انجام شد** → `true`
  - **خدمت انجام نشد** → `false`
- مقدار را حتماً به‌صورت boolean ارسال کنید؛ `patientReceivedService` نباید حذف یا `null` شود.
- ثبت نتیجه قبل از زمان مراجعه مجاز نیست؛ دکمه تا رسیدن زمان `reservationAt` غیرفعال باشد.
- برای رزرو لغوشده یا قبلاً بررسی‌شده، عملیات را نمایش ندهید.
- endpoint به permission با نام `ConfirmAttendance` نیاز دارد.
- فیلد قدیمی `approved` موقتاً برای سازگاری نسخه قبلی پشتیبانی می‌شود، اما کد جدید فرانت فقط باید `patientReceivedService` را ارسال کند.

پیام‌های مهم خطای API که باید به کاربر نمایش داده شوند:

- `وضعیت انجام یا عدم انجام خدمت باید مشخص شود`
- `نتیجه خدمت فقط بعد از زمان مراجعه قابل ثبت است`
- `رزرو لغو شده قابل بررسی نیست`
- `بررسی این رزرو قبلا ثبت شده است`

---

## ۳. لیست رزروهای منشی و مرتب‌سازی وقت بیمار

ترتیب پیش‌فرض API صعودی و بر اساس `reservationAt` است؛ یعنی در هر روز وقت‌های زودتر قبل از وقت‌های دیرتر قرار می‌گیرند. برای رکوردهایی با ساعت یکسان، `id` معیار دوم است تا ترتیب بین صفحه‌ها تغییر نکند.

```http
GET /api/Reservation/SecretaryReservations?date=2026-08-25&sortDirection=asc&pageNumber=1&pageSize=20
```

- `sortDirection=asc`: نزدیک‌ترین/زودترین وقت ابتدا؛ حالت پیش‌فرض و پیشنهادشده برای نمای روزانه.
- `sortDirection=desc`: دیرترین وقت ابتدا.
- برای صفحه روزانه، `date` را ارسال کنید تا رزروهای همان روز دریافت شوند.
- فرانت نباید آیتم‌های هر صفحه را دوباره بر اساس `createdAt` مرتب کند.

### فیلدهای جدید هر آیتم

```json
{
  "id": 1202,
  "reservationId": 1202,
  "reservationAt": "2026-08-25T10:30:00",
  "appointmentDateTime": "2026-08-25T10:30:00",
  "createdAt": "2026-08-19T09:15:00Z",
  "reservationAtPersian": "1405/06/03 10:30:00",
  "createdAtPersian": "1405/05/28 09:15:00",
  "reservationType": 2,
  "patientReceivedService": null,
  "secretaryReviewedAt": null,
  "secretaryReviewedAtPersian": null
}
```

معنای `patientReceivedService`:

| مقدار | نمایش پیشنهادی |
|---|---|
| `true` | خدمت انجام شد |
| `false` | خدمت انجام نشد |
| `null` | هنوز بررسی نشده |

---

## ۴. تاریخ‌های شمسی

### داشبورد منشی

برای متن قابل مشاهده به کاربر از این فیلدها استفاده کنید:

- `reservationAtPersian`
- `createdAtPersian`
- `secretaryReviewedAtPersian`

فیلدهای اصلی ISO را حذف نکنید؛ آن‌ها برای مقایسه زمان، غیرفعال کردن دکمه ثبت نتیجه و مقدار اولیه فرم ویرایش لازم‌اند.

تابع fallback پیشنهادی:

```ts
const displayDate = (persian: string | null | undefined, iso: string) =>
  persian || new Intl.DateTimeFormat("fa-IR-u-ca-persian", {
    dateStyle: "short",
    timeStyle: "short",
    timeZone: "Asia/Tehran",
  }).format(new Date(iso));
```

### گزارش روزانه ادمین

در `GET /api/admin/reports/daily-reservations` فیلدهای زیر برای نمایش اضافه شده‌اند:

- سطح گزارش: `datePersian` و `generatedAtPersian`
- هر آیتم: `appointmentDateTimePersian` و `createdAtPersian`

در UI گزارش از فیلدهای شمسی استفاده کنید. فایل‌های CSV نیز تاریخ شمسی دارند و تاریخ موجود در نام فایل‌های گزارش به تقویم شمسی است.

> ورودی queryهای تاریخ فعلاً مطابق قرارداد ASP.NET و به‌صورت میلادی `YYYY-MM-DD` ارسال می‌شود. تبدیل date picker شمسی به مقدار ISO باید قبل از ارسال درخواست انجام شود.

---

## ۵. مدل پیشنهادی TypeScript

```ts
export interface SecretaryReservationItem {
  id: number;
  reservationId: number;
  leadAssignmentId: number;
  consultantProfileId: number;
  patientName: string;
  patientPhoneNumber: string;
  reservationAt: string;
  appointmentDateTime: string;
  createdAt: string;
  reservationAtPersian: string;
  createdAtPersian: string;
  reservationType: ReservationType;
  patientReceivedService: boolean | null;
  secretaryReviewedAt: string | null;
  secretaryReviewedAtPersian: string | null;
  attendanceConfirmationStatus: number;
  isCanceled: boolean;
}

export interface SecretaryDashboardSummary {
  needCall: number;
  confirmed: number;
  noAnswer: number;
  cancelled: number;
  afterSalesServices: number;
}

export interface ReviewAttendanceRequest {
  reservationId: number;
  patientReceivedService: boolean;
  note?: string;
}
```

## چک‌لیست تحویل فرانت

- [ ] enum نوع رزرو اضافه شده است.
- [ ] تب/کارت «خدمات پس از فروش» و badge تعداد آن اضافه شده است.
- [ ] لیست این تب با `reservationType=2` درخواست می‌شود.
- [ ] فرم ثبت وقت خدمات پس از فروش `reservationType: 2` ارسال می‌کند.
- [ ] سؤال نتیجه مراجعه بر اساس «انجام شدن خدمت» طراحی شده است.
- [ ] درخواست بررسی با `patientReceivedService` ارسال می‌شود.
- [ ] رزروهای روز بر اساس `reservationAt` و ترتیب API نمایش داده می‌شوند.
- [ ] همه تاریخ‌های قابل مشاهده از فیلد شمسی متناظر خوانده می‌شوند.
- [ ] حالت‌های `403` برای permissionهای `ViewReservations`، `CreateReservation` و `ConfirmAttendance` مدیریت شده‌اند.
