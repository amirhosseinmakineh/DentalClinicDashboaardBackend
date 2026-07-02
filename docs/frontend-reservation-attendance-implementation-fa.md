# مستند پیاده‌سازی فرانت: سیستم رزرو، تایید حضور بیمار و امتیاز مشاور

> **نسخه:** 2026-07-02  
> **مخاطب:** تیم فرانت‌اند  
> **Base URL:** `{API_BASE}/api`

این سند مرجع اصلی پیاده‌سازی است. همه endpointها، stateها، صفحات، منطق دکمه‌ها و سناریوهای بیزینسی در همین فایل آمده است.

---

## فهرست

1. [خلاصه بیزینسی](#۱-خلاصه-بیزینسی)
2. [نقش‌ها و مسئولیت‌ها](#۲-نقش‌ها-و-مسئولیت‌ها)
3. [جریان کامل سیستم](#۳-جریان-کامل-سیستم)
4. [ماشین وضعیت (State Machine)](#۴-ماشین-وضعیت)
5. [ساختار پاسخ API](#۵-ساختار-پاسخ-api)
6. [TypeScript Types](#۶-typescript-types)
7. [صفحات و کامپوننت‌ها — مشاور](#۷-صفحات-مشاور)
8. [صفحات و کامپوننت‌ها — منشی](#۸-صفحات-منشی)
9. [مرجع کامل API](#۹-مرجع-کامل-api)
10. [منطق UI و دکمه‌ها](#۱۰-منطق-ui-و-دکمه‌ها)
11. [سناریوهای کاربری](#۱۱-سناریوهای-کاربری)
12. [مدیریت خطا](#۱۲-مدیریت-خطا)
13. [Polling و رفرش](#۱۳-polling-و-رفرش)
14. [چک‌لیست پیاده‌سازی](#۱۴-چک‌لیست-پیاده‌سازی)
15. [نمونه Service Layer](#۱۵-نمونه-service-layer)

---

## ۱. خلاصه بیزینسی

### مشکل

کلینیک نیاز دارد بعد از تماس موفق مشاور با بیمار، وقت ویزیت ثبت شود. در روز ویزیت باید:
1. مشاور اعلام کند بیمار آمده یا نیامده.
2. منشی اظهار مشاور را بررسی و تایید/رد کند.
3. بر اساس تایید منشی، امتیاز به مشاور اضافه یا کم شود.

### راه‌حل

| مرحله | انجام‌دهنده | عمل |
|---:|---|---|
| 1 | مشاور | ثبت گزارش تماس موفق لید |
| 2 | مشاور | ثبت رزرو با تاریخ/ساعت |
| 3 | منشی | (اختیاری) تکمیل پرونده بیمار |
| 4 | مشاور | بعد از رسیدن `reservationAt` → تایید حضور |
| 5 | منشی | بررسی اظهار مشاور → امتیاز ±۱۰ |

### قوانین بیزینسی مهم

| قانون | توضیح |
|---|---|
| رزرو فقط برای لید تماس‌موفق | `callResult` باید `Contacted(1)` یا `Converted(2)` باشد |
| زمان رزرو باید آینده باشد | `reservationAt > now` |
| هر لید فقط یک رزرو فعال | رزرو تکراری برای همان لید مجاز نیست |
| ظرفیت مشاور | حداکثر ۱۰ رزرو فعال در یک `reservationAt` |
| تایید حضور مشاور | فقط وقتی `reservationAt <= now` |
| بررسی منشی | فقط بعد از تایید مشاور |
| امتیاز | +۱۰ تایید منشی، -۱۰ رد منشی، یک‌بار برای هر رزرو |
| تایید تکراری مشاور | مجاز نیست (بعد از ثبت اول، API خطا می‌دهد) |

---

## ۲. نقش‌ها و مسئولیت‌ها

### مشاور (Consultant)

| صفحه | مسئولیت |
|---|---|
| ثبت رزرو | بعد از تماس موفق، وقت ویزیت تعیین کند |
| لیست رزروها | همه رزروهای خود را ببیند |
| تایید حضور | در روز ویزیت اعلام کند بیمار آمد/نیامد |
| رزروهای انجام‌شده | رزروهایی که منشی بررسی کرده + امتیاز نهایی |

**شناسه مورد نیاز:** `consultantProfileId` (از پروفایل لاگین‌شده)

### منشی (Secretary)

| صفحه | مسئولیت |
|---|---|
| لیست رزرو همه مشاورها | جستجو، فیلتر، صفحه‌بندی |
| تکمیل پرونده بیمار | اگر `requiresPatientProfile=true` |
| بررسی حضور | تایید/رد اظهار مشاور + اعمال امتیاز |

**شناسه مورد نیاز:** `secretaryUserId` (از کاربر لاگین‌شده)

---

## ۳. جریان کامل سیستم

```
[تماس موفق لید]
       │
       ▼
POST /api/Consultant/SubmitLeadCallReport  (callResult = Contacted یا Converted)
       │
       ▼
POST /api/Reservation  (ثبت رزرو با reservationAt آینده)
       │
       ├──► [منشی] POST /api/Reservation/CompletePatientProfile  (اگر پرونده ندارد)
       │
       ▼
[انتظار تا reservationAt]
       │
       ▼
[مشاور] GET /api/Reservation/DueConfirmations  → دکمه «بیمار آمد / نیامد» فعال
       │
       ▼
[مشاور] POST /api/Reservation/ConfirmAttendance
       │
       ▼
وضعیت = ConsultantConfirmedPresent(2) یا ConsultantConfirmedAbsent(3)
       │
       ▼
[منشی] GET /api/Reservation/SecretaryReservations?onlyWaitingForSecretaryReview=true
       │
       ▼
[منشی] POST /api/Reservation/ReviewAttendance
       │
       ├── approved=true  → SecretaryApproved(4)  → +10 امتیاز
       └── approved=false → SecretaryRejected(5) → -10 امتیاز
```

---

## ۴. ماشین وضعیت

### Enum: `ReservationAttendanceConfirmationStatus`

```typescript
enum ReservationAttendanceConfirmationStatus {
  PendingConsultantConfirmation = 1,  // در انتظار تایید مشاور
  ConsultantConfirmedPresent = 2,     // مشاور: بیمار آمده
  ConsultantConfirmedAbsent = 3,      // مشاور: بیمار نیامده
  SecretaryApproved = 4,              // منشی تایید کرد (+امتیاز)
  SecretaryRejected = 5               // منشی رد کرد (-امتیاز)
}
```

### برچسب فارسی وضعیت (برای UI)

```typescript
const ATTENDANCE_STATUS_LABELS: Record<number, { label: string; color: string }> = {
  1: { label: 'در انتظار تایید مشاور', color: 'warning' },
  2: { label: 'مشاور: بیمار آمده — منتظر منشی', color: 'info' },
  3: { label: 'مشاور: بیمار نیامده — منتظر منشی', color: 'info' },
  4: { label: 'تایید شده توسط منشی', color: 'success' },
  5: { label: 'رد شده توسط منشی', color: 'error' },
};
```

### دیاگرام انتقال وضعیت

```
                    ┌─────────────────────────────────────┐
                    │  1 - PendingConsultantConfirmation   │
                    └──────────────┬──────────────────────┘
                                   │
              ConfirmAttendance    │    (فقط اگر reservationAt <= now)
                    ┌──────────────┴──────────────┐
                    ▼                             ▼
     2 - ConsultantConfirmedPresent    3 - ConsultantConfirmedAbsent
                    │                             │
                    └──────────────┬──────────────┘
                                   │
                          ReviewAttendance
                    ┌──────────────┴──────────────┐
                    ▼                             ▼
          4 - SecretaryApproved          5 - SecretaryRejected
             (+10 امتیاز)                  (-10 امتیاز)
```

---

## ۵. ساختار پاسخ API

### پاسخ صفحه‌بندی‌شده

```typescript
interface PaginatedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number; // محاسبه‌شده در بک‌اند
}
```

### پاسخ Command (ثبت/ویرایش)

```typescript
interface ApiResult<T = void> {
  isSuccess: boolean;
  message: string;
  data?: T;
}
```

**نکته:** همیشه `isSuccess` را چک کنید. پیام خطا در `message` است.

---

## ۶. TypeScript Types

```typescript
// ─── مشترک ───

interface ReservationItemBase {
  id: number;
  leadAssignmentId: number;
  consultantProfileId: number;
  patientUserId: string | null;
  requiresPatientProfile: boolean;
  reservationAt: string; // ISO DateTime
  patientName: string;
  patientPhoneNumber: string;
  secondaryPhoneNumber: string | null;
  patientCity: string;
  patientRegion: string | null;
  businessName: string | null;
  attendanceProbabilityPercent: number | null;
  attendanceConfirmationStatus: ReservationAttendanceConfirmationStatus;
  consultantAttendanceConfirmedAt: string | null;
  consultantSaysPatientAttended: boolean | null;
  consultantAttendanceNote: string | null;
  secretaryReviewedAt: string | null;
  secretaryUserId: string | null;
  secretaryApprovedConsultantConfirmation: boolean | null;
  secretaryReviewNote: string | null;
  isAttendanceScoreApplied: boolean;
  attendanceScoreValue: number | null;
  attendanceScoreAppliedAt: string | null;
  description: string | null;
  isCanceled: boolean;
}

// ─── مشاور ───

interface ConsultantReservationItem extends ReservationItemBase {
  isDueForConsultantConfirmation: boolean;
}

// ─── منشی ───

interface SecretaryReservationItem extends ReservationItemBase {
  consultantUserId: string;
  consultantFullName: string;
  isWaitingForSecretaryReview: boolean;
  isReservationDue: boolean;
}

// ─── Dashboard مشاور ───

interface ConsultantDashboardStatus {
  profileId: number;
  isAvailable: boolean;
  isOnline: boolean;
  lastOnlineAt: string | null;
  lastOfflineAt: string | null;
  pendingOfflineLeadCount: number;
  currentScore: number;
  canGoOnline: boolean;
  onlineStatusBlockReason: string | null;
}
```

---

## ۷. صفحات مشاور

### ۷.۱ صفحه «ثبت رزرو»

**مسیر پیشنهادی:** `/consultant/reservations/create`  
**ورود از:** صفحه گزارش تماس موفق (`ShouldOpenReservationPage=true`)

#### API

```
POST /api/Reservation
```

#### Request Body

```json
{
  "leadAssignmentId": 12,
  "consultantProfileId": 49,
  "reservationAt": "2026-07-10T14:30:00",
  "description": "توضیح اختیاری"
}
```

| فیلد | نوع | الزامی | اعتبارسنجی فرانت |
|---|---|:---:|---|
| `leadAssignmentId` | number | ✅ | از لید جاری |
| `consultantProfileId` | number | ✅ | از پروفایل لاگین |
| `reservationAt` | DateTime | ✅ | باید > الان باشد |
| `description` | string | ❌ | حداکثر ۵۰۰ کاراکتر |

#### Response موفق

```json
{
  "isSuccess": true,
  "message": "رزرو با موفقیت ثبت شد",
  "data": {
    "id": 123,
    "leadAssignmentId": 12,
    "consultantProfileId": 49,
    "patientUserId": null,
    "requiresPatientProfile": true,
    "reservationAt": "2026-07-10T14:30:00",
    "patientName": "علی احمدی",
    "patientPhoneNumber": "09120000000",
    "secondaryPhoneNumber": "02100000000",
    "patientCity": "تهران",
    "patientRegion": "سعادت‌آباد",
    "businessName": "اینستاگرام",
    "attendanceProbabilityPercent": 80,
    "attendanceConfirmationStatus": 1,
    "patientName": "علی احمدی",
    "patientPhoneNumber": "09120000000"
  }
}
```

#### خطاهای بیزینسی

| message | اقدام UI |
|---|---|
| `زمان رزرو باید در آینده باشد` | هایلایت datepicker |
| `فقط لیدهای تماس موفق قابل رزرو هستند` | بستن فرم + redirect به لیدها |
| `برای این بیمار قبلا رزرو فعال ثبت شده است` | نمایش لینک به رزرو موجود |
| `ظرفیت این بازه زمانی برای مشاور تکمیل است` | پیشنهاد زمان دیگر |

---

### ۷.۲ صفحه «رزروهای من» (لیست اصلی)

**مسیر پیشنهادی:** `/consultant/reservations`

#### ساختار UI

```
┌─────────────────────────────────────────────────────────────┐
│  رزروهای من                                    [+ رزرو جدید] │
├─────────────────────────────────────────────────────────────┤
│  [در انتظار تایید]  [همه]  [انجام‌شده]                      │
├─────────────────────────────────────────────────────────────┤
│  از تاریخ: [____]  تا تاریخ: [____]  جستجو: [________]     │
├─────────────────────────────────────────────────────────────┤
│  جدول رزروها                                                │
│  ┌──────┬────────┬──────────┬──────────┬─────────┬────────┐ │
│  │ زمان │ بیمار  │ موبایل   │ وضعیت    │ امتیاز  │ عملیات │ │
│  └──────┴────────┴──────────┴──────────┴─────────┴────────┘ │
│  صفحه‌بندی: < 1 2 3 >                                       │
└─────────────────────────────────────────────────────────────┘
```

#### API هر تب

| تب | API | Query Params |
|---|---|---|
| **در انتظار تایید** | `GET /api/Reservation/DueConfirmations` | `consultantProfileId` |
| **همه** | `GET /api/Reservation/GetConsultantReservations` | `consultantProfileId`, `from`, `to`, `patientName`, `pageNumber`, `pageSize` |
| **انجام‌شده** | `GET /api/Reservation/GetConsultantReservations` | `consultantProfileId`, `onlySecretaryReviewed=true` |

#### ستون‌های جدول

| ستون | فیلد | فرمت نمایش |
|---|---|---|
| زمان رزرو | `reservationAt` | شمسی + ساعت |
| نام بیمار | `patientName` | متن |
| موبایل | `patientPhoneNumber` | LTR |
| شهر | `patientCity` | متن |
| احتمال حضور | `attendanceProbabilityPercent` | `80%` یا `-` |
| وضعیت | `attendanceConfirmationStatus` | badge رنگی (جدول بخش ۴) |
| امتیاز | `attendanceScoreValue` | `+10` / `-10` / `-` |
| عملیات | — | دکمه‌های شرطی (بخش ۱۰) |

---

### ۷.۳ مودال «تایید حضور بیمار»

**باز شدن:** کلیک روی «بیمار آمد» یا «بیمار نیامد» در تب «در انتظار تایید»

#### API

```
POST /api/Reservation/ConfirmAttendance
```

#### Request Body

```json
{
  "reservationId": 123,
  "consultantProfileId": 49,
  "patientAttended": true,
  "note": "بیمار سر وقت مراجعه کرد"
}
```

| فیلد | نوع | الزامی |
|---|---|:---:|
| `reservationId` | number | ✅ |
| `consultantProfileId` | number | ✅ |
| `patientAttended` | boolean | ✅ |
| `note` | string | ❌ |

#### UI مودال

```
┌──────────────────────────────────────┐
│  تایید حضور بیمار                    │
├──────────────────────────────────────┤
│  بیمار: علی احمدی                    │
│  زمان رزرو: ۱۴۰۵/۰۴/۱۲ - ۱۴:۳۰      │
│  موبایل: 09120000000                 │
├──────────────────────────────────────┤
│  توضیحات (اختیاری):                  │
│  [________________________________]  │
├──────────────────────────────────────┤
│  [بیمار نیامد]          [بیمار آمد]  │
└──────────────────────────────────────┘
```

- دکمه «بیمار آمد» → `patientAttended: true`
- دکمه «بیمار نیامد» → `patientAttended: false`
- قبل از ارسال: confirm dialog

#### بعد از موفقیت

1. Toast: `تایید حضور بیمار ثبت شد و در انتظار بررسی منشی است`
2. حذف آیتم از تب «در انتظار تایید»
3. رفرش لیست «همه»

---

### ۷.۴ صفحه «جزئیات رزرو» (اختیاری)

**مسیر:** `/consultant/reservations/:id`

نمایش تمام فیلدهای `ConsultantReservationItem` + timeline وضعیت:

```
● رزرو ثبت شد                    → reservationAt
● مشاور تایید کرد: بیمار آمد     → consultantAttendanceConfirmedAt
● منشی تایید کرد (+10 امتیاز)    → secretaryReviewedAt
```

---

## ۸. صفحات منشی

### ۸.۱ صفحه «داشبورد رزروها»

**مسیر پیشنهادی:** `/secretary/reservations`

#### ساختار UI

```
┌──────────────────────────────────────────────────────────────────┐
│  مدیریت رزروها                                                    │
├──────────────────────────────────────────────────────────────────┤
│  [صف بررسی (3)]  [همه رزروها]  [انجام‌شده]                       │
├──────────────────────────────────────────────────────────────────┤
│  جستجو: [________________________]  مشاور: [▼ همه]              │
│  از: [____]  تا: [____]  وضعیت: [▼ همه]                         │
├──────────────────────────────────────────────────────────────────┤
│  جدول                                                            │
│  ┌──────┬────────┬────────┬──────────┬────────┬────────┬──────┐ │
│  │ زمان │ بیمار  │ مشاور  │ وضعیت    │ پرونده │ امتیاز │ عمل  │ │
│  └──────┴────────┴────────┴──────────┴────────┴────────┴──────┘ │
└──────────────────────────────────────────────────────────────────┘
```

#### API هر تب

| تب | API | Query Params |
|---|---|---|
| **صف بررسی** | `GET /api/Reservation/SecretaryReservations` | `onlyWaitingForSecretaryReview=true`, `onlyDue=true`, `pageNumber`, `pageSize` |
| **همه** | `GET /api/Reservation/SecretaryReservations` | `searchText`, `consultantProfileId`, `from`, `to`, `pageNumber`, `pageSize` |
| **انجام‌شده** | `GET /api/Reservation/SecretaryReservations` | `attendanceConfirmationStatus=4` یا فیلتر ۵ در فرانت |

#### Badge تعداد صف بررسی

```typescript
// برای نمایش روی تب «صف بررسی»
const { totalCount } = await api.getSecretaryReservations({
  onlyWaitingForSecretaryReview: true,
  onlyDue: true,
  pageSize: 1,
});
// totalCount → badge number
```

#### Dropdown مشاور

```
GET /api/Consultant/GetConsultants?pageNumber=1&pageSize=100
```

Response item:

```json
{
  "id": "user-guid",
  "profileId": 49,
  "firstName": "سارا",
  "lastName": "محمدی",
  "phoneNumber": "09120000000"
}
```

**نکته:** برای فیلتر رزرو از `profileId` استفاده کنید، نه `id`.

---

### ۸.۲ مودال «تکمیل پرونده بیمار»

**شرط نمایش:** `requiresPatientProfile === true`

#### API

```
POST /api/Reservation/CompletePatientProfile
```

#### Request Body

```json
{
  "reservationId": 123,
  "firstName": "علی",
  "lastName": "احمدی",
  "phoneNumber": "09120000000",
  "passwordHash": "123456",
  "avatarImageName": null,
  "gender": 1,
  "birthDate": "1995-01-01T00:00:00",
  "nationalCode": "0012345678",
  "address": "تهران، خیابان ولیعصر",
  "emergencyPhoneNumber": "09121111111",
  "insuranceName": "تامین اجتماعی",
  "notes": "توضیحات"
}
```

| فیلد | نوع | الزامی | توضیح |
|---|---|:---:|---|
| `reservationId` | number | ✅ | |
| `firstName` | string | ✅ | |
| `lastName` | string | ✅ | |
| `phoneNumber` | string | ✅ | **باید دقیقاً برابر `patientPhoneNumber` لید باشد** |
| `passwordHash` | string | ✅ | رمز عبور بیمار (بک‌اند hash می‌کند) |
| `gender` | number | ✅ | `1=Male`, `2=Female` |
| `birthDate` | DateTime | ✅ | |
| `nationalCode` | string | ✅ | |
| `address` | string | ✅ | |
| `emergencyPhoneNumber` | string | ❌ | |
| `insuranceName` | string | ❌ | |
| `notes` | string | ❌ | |

**UX:** `phoneNumber` را از `patientPhoneNumber` رزرو pre-fill کنید و readonly بگذارید.

---

### ۸.۳ مودال «بررسی اظهار مشاور»

**شرط نمایش:** `isWaitingForSecretaryReview === true && isReservationDue === true`

#### API

```
POST /api/Reservation/ReviewAttendance
```

#### Request Body — تایید (موافقم با مشاور)

```json
{
  "reservationId": 123,
  "secretaryUserId": "secretary-user-guid",
  "approved": true,
  "note": "بیمار در کلینیک حضور داشت"
}
```

#### Request Body — رد (مخالفم با مشاور)

```json
{
  "reservationId": 123,
  "secretaryUserId": "secretary-user-guid",
  "approved": false,
  "note": "بیمار در کلینیک دیده نشد"
}
```

#### UI مودال

```
┌──────────────────────────────────────────────┐
│  بررسی حضور بیمار                             │
├──────────────────────────────────────────────┤
│  بیمار: علی احمدی                             │
│  مشاور: سارا محمدی                            │
│  زمان رزرو: ۱۴۰۵/۰۴/۱۲ - ۱۴:۳۰               │
├──────────────────────────────────────────────┤
│  اظهار مشاور:                                 │
│  ┌──────────────────────────────────────────┐ │
│  │ ✅ بیمار آمده است                        │ │
│  │ توضیح: بیمار سر وقت مراجعه کرد          │ │
│  │ زمان ثبت: ۱۴۰۵/۰۴/۱۲ - ۱۴:۴۵             │ │
│  └──────────────────────────────────────────┘ │
├──────────────────────────────────────────────┤
│  یادداشت منشی (اختیاری):                      │
│  [________________________________________]   │
├──────────────────────────────────────────────┤
│  [رد اظهار مشاور (-10)]  [تایید اظهار (+10)] │
└──────────────────────────────────────────────┘
```

**نمایش اظهار مشاور:**

```typescript
const consultantStatement = item.consultantSaysPatientAttended
  ? '✅ بیمار آمده است'
  : '❌ بیمار نیامده است';
```

#### منطق امتیاز (برای نمایش به منشی قبل از تایید)

| دکمه | `approved` | امتیاز مشاور | وضعیت نهایی |
|---|---|---|---|
| تایید اظهار مشاور | `true` | **+10** | `SecretaryApproved (4)` |
| رد اظهار مشاور | `false` | **-10** | `SecretaryRejected (5)` |

**مثال بیزینسی:**
- مشاور گفته «آمده» (`consultantSaysPatientAttended=true`) ولی منشی می‌گوید «نیامده» → `approved=false` → **-10**
- مشاور گفته «نیامده» ولی منشی می‌گوید «آمده» → `approved=false` → **-10**

---

## ۹. مرجع کامل API

### ۹.۱ `GET /api/Reservation/GetConsultantReservations`

لیست رزروهای یک مشاور.

| Query | نوع | پیش‌فرض | توضیح |
|---|---|---|---|
| `consultantProfileId` | long | **الزامی** | |
| `from` | DateTime? | — | `reservationAt >= from` |
| `to` | DateTime? | — | `reservationAt <= to` |
| `attendanceConfirmationStatus` | int? | — | ۱ تا ۵ |
| `onlyDueForConsultantConfirmation` | bool | false | فقط آماده تایید حضور |
| `onlySecretaryReviewed` | bool | false | فقط انجام‌شده (۴ و ۵) |
| `patientName` | string? | — | جستجو |
| `patientPhoneNumber` | string? | — | جستجو |
| `includeCanceled` | bool | false | |
| `pageNumber` | int | 1 | |
| `pageSize` | int | 10 | حداکثر ۱۰۰ |

**Response:** `PaginatedResult<ConsultantReservationItem>`

---

### ۹.۲ `GET /api/Reservation/DueConfirmations`

رزروهایی که دکمه تایید حضور مشاور باید فعال باشد.

| Query | نوع | توضیح |
|---|---|---|
| `consultantProfileId` | long | **الزامی** |
| `now` | DateTime? | override برای تست |

**Response:** `ConsultantReservationItem[]` (بدون صفحه‌بندی)

**فیلتر بک‌اند:**
- `!isCanceled`
- `reservationAt <= now`
- `attendanceConfirmationStatus === 1`

---

### ۹.۳ `GET /api/Reservation/SecretaryReservations`

لیست رزروها برای منشی (همه مشاورها یا فیلترشده).

| Query | نوع | پیش‌فرض | توضیح |
|---|---|---|---|
| `consultantProfileId` | long? | — | null = همه |
| `from` | DateTime? | — | |
| `to` | DateTime? | — | |
| `attendanceConfirmationStatus` | int? | — | |
| `onlyWaitingForSecretaryReview` | bool | false | وضعیت ۲ یا ۳ |
| `onlyDue` | bool | false | `reservationAt <= now` |
| `patientName` | string? | — | |
| `patientPhoneNumber` | string? | — | |
| `consultantName` | string? | — | |
| `searchText` | string? | — | جستجوی ترکیبی |
| `includeCanceled` | bool | false | |
| `pageNumber` | int | 1 | |
| `pageSize` | int | 10 | حداکثر ۱۰۰ |

**Response:** `PaginatedResult<SecretaryReservationItem>`

---

### ۹.۴ `POST /api/Reservation/ConfirmAttendance`

| Body | نوع | الزامی |
|---|---|:---:|
| `reservationId` | long | ✅ |
| `consultantProfileId` | long | ✅ |
| `patientAttended` | bool | ✅ |
| `note` | string? | ❌ |

**Response:** `ApiResult` (بدون data)

---

### ۹.۵ `POST /api/Reservation/ReviewAttendance`

| Body | نوع | الزامی |
|---|---|:---:|
| `reservationId` | long | ✅ |
| `secretaryUserId` | Guid | ✅ |
| `approved` | bool | ✅ |
| `note` | string? | ❌ |

**Response:** `ApiResult` (بدون data)

---

### ۹.۶ `POST /api/Reservation` — ثبت رزرو

(جزئیات در بخش ۷.۱)

---

### ۹.۷ `POST /api/Reservation/CompletePatientProfile` — تکمیل پرونده

(جزئیات در بخش ۸.۲)

---

### ۹.۸ `GET /api/Consultant/GetDashboardStatus` — وضعیت داشبورد مشاور

| Query | نوع |
|---|---|
| `profileId` | long |

**Response:**

```json
{
  "profileId": 49,
  "isAvailable": true,
  "isOnline": false,
  "lastOnlineAt": "2026-07-02T15:28:01",
  "lastOfflineAt": "2026-07-02T19:59:29",
  "pendingOfflineLeadCount": 0,
  "currentScore": -95,
  "canGoOnline": true,
  "onlineStatusBlockReason": null
}
```

**نکته:** `pendingOfflineLeadCount` فقط لیدهایی را می‌شمارد که `reportSubmittedAt == null`. اگر مشاور همه لیدها را گزارش داده، باید `0` باشد.

---

## ۱۰. منطق UI و دکمه‌ها

### ۱۰.۱ مشاور — دکمه‌های هر ردیف

```typescript
function getConsultantRowActions(item: ConsultantReservationItem) {
  const actions = [];

  // تایید حضور — فقط در تب «در انتظار تایید»
  if (item.isDueForConsultantConfirmation && !item.isCanceled) {
    actions.push({ key: 'confirm-present', label: 'بیمار آمد', variant: 'success' });
    actions.push({ key: 'confirm-absent', label: 'بیمار نیامد', variant: 'danger' });
  }

  // نمایش وضعیت نهایی
  if (item.isAttendanceScoreApplied) {
    actions.push({
      key: 'score',
      label: item.attendanceScoreValue! > 0 ? `+${item.attendanceScoreValue} امتیاز` : `${item.attendanceScoreValue} امتیاز`,
      disabled: true,
    });
  }

  // در انتظار منشی
  if (
  item.attendanceConfirmationStatus === 2 ||
  item.attendanceConfirmationStatus === 3
  ) {
    actions.push({ key: 'waiting', label: 'منتظر بررسی منشی', disabled: true });
  }

  return actions;
}
```

### ۱۰.۲ منشی — دکمه‌های هر ردیف

```typescript
function getSecretaryRowActions(item: SecretaryReservationItem) {
  const actions = [];

  if (item.requiresPatientProfile && !item.isCanceled) {
    actions.push({ key: 'complete-profile', label: 'تکمیل پرونده', variant: 'primary' });
  }

  if (item.isWaitingForSecretaryReview && item.isReservationDue && !item.isCanceled) {
    actions.push({ key: 'approve', label: 'تایید اظهار مشاور', variant: 'success' });
    actions.push({ key: 'reject', label: 'رد اظهار مشاور', variant: 'danger' });
  }

  if (item.isAttendanceScoreApplied) {
    const sign = item.attendanceScoreValue! > 0 ? '+' : '';
    actions.push({
      key: 'score',
      label: `${sign}${item.attendanceScoreValue} امتیاز`,
      disabled: true,
    });
  }

  return actions;
}
```

### ۱۰.۳ غیرفعال بودن دکمه تایید حضور (قبل از زمان رزرو)

```typescript
function canConfirmAttendance(item: ConsultantReservationItem): boolean {
  return item.isDueForConsultantConfirmation && !item.isCanceled;
}

// در تب «همه» — نمایش tooltip
function getConfirmDisabledReason(item: ConsultantReservationItem): string | null {
  if (item.isCanceled) return 'رزرو لغو شده';
  if (new Date(item.reservationAt) > new Date()) return 'هنوز زمان رزرو نرسیده';
  if (item.attendanceConfirmationStatus !== 1) return 'قبلاً تایید شده';
  return null;
}
```

---

## ۱۱. سناریوهای کاربری

### سناریو ۱: رزرو و تایید موفق

| مرحله | نقش | عمل | API |
|---:|---|---|---|
| 1 | مشاور | تماس موفق | `SubmitLeadCallReport` |
| 2 | مشاور | رزرو فردا ۱۴:۳۰ | `POST /api/Reservation` |
| 3 | منشی | تکمیل پرونده | `CompletePatientProfile` |
| 4 | مشاور | فردا بعد ۱۴:۳۰ — «بیمار آمد» | `ConfirmAttendance` |
| 5 | منشی | «تایید اظهار مشاور» | `ReviewAttendance(approved=true)` |
| 6 | مشاور | می‌بیند +۱۰ امتیاز | `GetConsultantReservations?onlySecretaryReviewed=true` |

### سناریو ۲: مشاور گفت آمده، منشی رد کرد

| مرحله | وضعیت | امتیاز |
|---:|---|---|
| 1 | مشاور: `patientAttended=true` | — |
| 2 | وضعیت → `ConsultantConfirmedPresent (2)` | — |
| 3 | منشی: `approved=false` | **-10** |
| 4 | وضعیت → `SecretaryRejected (5)` | |

### سناریو ۳: بیمار نیامده — تایید منفی

| مرحله | عمل |
|---|---|
| 1 | مشاور: `patientAttended=false` |
| 2 | منشی موافق: `approved=true` → +10 |
| 3 | منشی مخالف: `approved=false` → -10 |

### سناریو ۴: زمان رزرو هنوز نرسیده

| شرط | UI |
|---|---|
| `reservationAt > now` | دکمه تایید حضور **غیرفعال** + tooltip «زمان رزرو: ...» |
| API | `DueConfirmations` این رزرو را **برنمی‌گرداند** |

---

## ۱۲. مدیریت خطا

### الگوی کلی

```typescript
async function handleApiCall<T>(promise: Promise<ApiResult<T>>) {
  const result = await promise;
  if (!result.isSuccess) {
    toast.error(result.message);
    return null;
  }
  toast.success(result.message);
  return result.data;
}
```

### جدول خطاهای رایج

#### ConfirmAttendance (مشاور)

| message | علت | UI |
|---|---|---|
| `رزرو برای این مشاور یافت نشد` | ID اشتباه | redirect |
| `رزرو لغو شده قابل تایید حضور نیست` | canceled | badge |
| `تایید حضور فقط بعد از رسیدن روز و ساعت رزرو ممکن است` | زودهنگام | refresh لیست |
| `تایید حضور این رزرو قبلا ثبت شده است` | تکراری | refresh |
| `این رزرو قبلا توسط منشی بررسی شده است` | تمام‌شده | refresh |

#### ReviewAttendance (منشی)

| message | علت | UI |
|---|---|---|
| `ابتدا مشاور باید حضور یا عدم حضور بیمار را تایید کند` | وضعیت ۱ | refresh |
| `امتیاز این بررسی قبلا اعمال شده است` | تکراری | disable دکمه |

#### CompletePatientProfile (منشی)

| message | علت | UI |
|---|---|---|
| `کد ملی بیمار الزامی است` | validation | highlight |
| `آدرس بیمار الزامی است` | validation | highlight |
| `شماره موبایل بیمار باید با شماره لید رزرو شده یکسان باشد` | mismatch | highlight phone |
| `کاربری با این شماره موبایل قبلاً ثبت شده است` | duplicate | error |

---

## ۱۳. Polling و رفرش

### مشاور — تب «در انتظار تایید»

```typescript
// هر ۶۰ ثانیه رفرش (برای فعال شدن دکمه وقتی reservationAt برسد)
useEffect(() => {
  const interval = setInterval(() => {
    refetchDueConfirmations();
  }, 60_000);
  return () => clearInterval(interval);
}, []);
```

### منشی — badge صف بررسی

```typescript
// هر ۳۰ ثانیه تعداد صف را آپدیت کن
useEffect(() => {
  const interval = setInterval(fetchReviewQueueCount, 30_000);
  return () => clearInterval(interval);
}, []);
```

### بعد از هر mutation

| عمل | رفرش |
|---|---|
| `ConfirmAttendance` | `DueConfirmations` + `GetConsultantReservations` |
| `ReviewAttendance` | `SecretaryReservations` (هر سه تب) |
| `CompletePatientProfile` | همان صفحه |
| `CreateReservation` | `GetConsultantReservations` |

---

## ۱۴. چک‌لیست پیاده‌سازی

### فاز ۱ — Types و Service

- [ ] تعریف enum `ReservationAttendanceConfirmationStatus`
- [ ] تعریف interfaceهای بخش ۶
- [ ] ساخت `reservationApi.ts` (بخش ۱۵)
- [ ] ساخت helper `getAttendanceStatusLabel()`

### فاز ۲ — مشاور

- [ ] صفحه ثبت رزرو (`POST /api/Reservation`)
- [ ] صفحه لیست رزرو با ۳ تب
- [ ] تب «در انتظار تایید» با `DueConfirmations`
- [ ] مودال تایید حضور (`ConfirmAttendance`)
- [ ] تب «انجام‌شده» با `onlySecretaryReviewed=true`
- [ ] نمایش امتیاز نهایی
- [ ] polling برای تب در انتظار

### فاز ۳ — منشی

- [ ] صفحه داشبورد رزرو با ۳ تب
- [ ] فیلتر جستجو (`searchText`) + مشاور + تاریخ + وضعیت
- [ ] badge تعداد صف بررسی
- [ ] مودال تکمیل پرونده (`CompletePatientProfile`)
- [ ] مودال بررسی اظهار مشاور (`ReviewAttendance`)
- [ ] نمایش اظهار مشاور قبل از تایید/رد
- [ ] dropdown مشاور از `GetConsultants`

### فاز ۴ — UX و Edge Cases

- [ ] tooltip برای دکمه‌های غیرفعال
- [ ] confirm dialog قبل از تایید/رد
- [ ] toast موفقیت/خطا
- [ ] empty state هر تب
- [ ] loading skeleton
- [ ] فرمت تاریخ شمسی

---

## ۱۵. نمونه Service Layer

```typescript
// services/reservationApi.ts

const BASE = '/api/Reservation';

export const reservationApi = {
  // ─── مشاور ───

  createReservation(body: {
    leadAssignmentId: number;
    consultantProfileId: number;
    reservationAt: string;
    description?: string;
  }) {
    return post<CreateReservationResponse>(BASE, body);
  },

  getConsultantReservations(params: {
    consultantProfileId: number;
    from?: string;
    to?: string;
    attendanceConfirmationStatus?: number;
    onlyDueForConsultantConfirmation?: boolean;
    onlySecretaryReviewed?: boolean;
    patientName?: string;
    patientPhoneNumber?: string;
    pageNumber?: number;
    pageSize?: number;
  }) {
    return get<PaginatedResult<ConsultantReservationItem>>(
      `${BASE}/GetConsultantReservations`,
      params,
    );
  },

  getDueConfirmations(consultantProfileId: number) {
    return get<ConsultantReservationItem[]>(
      `${BASE}/DueConfirmations`,
      { consultantProfileId },
    );
  },

  confirmAttendance(body: {
    reservationId: number;
    consultantProfileId: number;
    patientAttended: boolean;
    note?: string;
  }) {
    return post<void>(`${BASE}/ConfirmAttendance`, body);
  },

  // ─── منشی ───

  getSecretaryReservations(params: {
    consultantProfileId?: number;
    from?: string;
    to?: string;
    attendanceConfirmationStatus?: number;
    onlyWaitingForSecretaryReview?: boolean;
    onlyDue?: boolean;
    patientName?: string;
    patientPhoneNumber?: string;
    consultantName?: string;
    searchText?: string;
    pageNumber?: number;
    pageSize?: number;
  }) {
    return get<PaginatedResult<SecretaryReservationItem>>(
      `${BASE}/SecretaryReservations`,
      params,
    );
  },

  completePatientProfile(body: CompletePatientProfileRequest) {
    return post<CompletePatientProfileResponse>(
      `${BASE}/CompletePatientProfile`,
      body,
    );
  },

  reviewAttendance(body: {
    reservationId: number;
    secretaryUserId: string;
    approved: boolean;
    note?: string;
  }) {
    return post<void>(`${BASE}/ReviewAttendance`, body);
  },
};
```

```typescript
// hooks/useConsultantReservations.ts

export function useConsultantReservations(profileId: number, tab: 'due' | 'all' | 'completed') {
  return useQuery({
    queryKey: ['consultant-reservations', profileId, tab],
    queryFn: () => {
      if (tab === 'due') {
        return reservationApi.getDueConfirmations(profileId);
      }
      return reservationApi.getConsultantReservations({
        consultantProfileId: profileId,
        onlySecretaryReviewed: tab === 'completed',
        pageNumber: 1,
        pageSize: 20,
      });
    },
    refetchInterval: tab === 'due' ? 60_000 : false,
  });
}
```

```typescript
// hooks/useSecretaryReservations.ts

export function useSecretaryReservations(
  tab: 'review' | 'all' | 'completed',
  filters: SecretaryFilters,
) {
  const params = {
    pageNumber: filters.page,
    pageSize: 20,
    searchText: filters.search || undefined,
    consultantProfileId: filters.consultantProfileId || undefined,
    from: filters.from || undefined,
    to: filters.to || undefined,
    ...(tab === 'review' && {
      onlyWaitingForSecretaryReview: true,
      onlyDue: true,
    }),
    ...(tab === 'completed' && {
      attendanceConfirmationStatus: 4, // یا 5 — یا هر دو در فرانت
    }),
  };

  return useQuery({
    queryKey: ['secretary-reservations', tab, params],
    queryFn: () => reservationApi.getSecretaryReservations(params),
    refetchInterval: tab === 'review' ? 30_000 : false,
  });
}
```

---

## پیوست: نگاشت فیلد Response به UI

| فیلد API | نمایش در UI | فرمت |
|---|---|---|
| `reservationAt` | زمان رزرو | `1405/04/12 - 14:30` |
| `patientName` | نام بیمار | |
| `patientPhoneNumber` | موبایل | LTR |
| `secondaryPhoneNumber` | تلفن دوم | LTR |
| `attendanceProbabilityPercent` | احتمال حضور | `80%` |
| `consultantFullName` | مشاور (منشی) | |
| `requiresPatientProfile` | وضعیت پرونده | `تکمیل نشده` / `تکمیل شده` |
| `attendanceConfirmationStatus` | وضعیت | badge |
| `consultantSaysPatientAttended` | اظهار مشاور | `آمده` / `نیامده` |
| `attendanceScoreValue` | امتیاز | `+10` / `-10` |
| `isDueForConsultantConfirmation` | فعال بودن دکمه (مشاور) | boolean |
| `isWaitingForSecretaryReview` | نمایش دکمه بررسی (منشی) | boolean |
| `isReservationDue` | زمان رسیده (منشی) | boolean |

---

**پایان مستند.** برای سوالات فنی بک‌اند به `RESERVATION_API_DOCUMENTATION.md` مراجعه کنید.
