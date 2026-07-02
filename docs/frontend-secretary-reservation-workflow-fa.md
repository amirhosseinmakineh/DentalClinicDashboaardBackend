# مستند فرانت‌اند: داشبورد منشی — رزرو، تایید حضور و امتیاز مشاور

این مستند برای پیاده‌سازی داشبورد منشی، لیست رزروها، جستجو/فیلتر و بررسی حضور بیمار آماده شده است.

## جریان کلی

1. مشاور بعد از تماس موفق، رزرو بیمار را ثبت می‌کند.
2. منشی می‌تواند برای همان رزرو پرونده بیمار را تکمیل کند (`CompletePatientProfile`).
3. داشبورد منشی لیست رزروهای **همه مشاورها** را با جستجو و فیلتر نمایش می‌دهد.
4. بعد از رسیدن `reservationAt`، مشاور حضور/عدم حضور را ثبت می‌کند.
5. رزرو وارد صف بررسی منشی می‌شود (`isWaitingForSecretaryReview=true`).
6. منشی اظهار مشاور را تایید یا رد می‌کند:
   - تایید (`approved=true`) → **+۱۰** امتیاز به مشاور
   - رد (`approved=false`) → **-۱۰** امتیاز از مشاور (مثلاً مشاور گفته آمده، منشی گفته نیامده)

## Enum وضعیت تایید حضور

`ReservationAttendanceConfirmationStatus`:

| مقدار | نام | معنی |
|---:|---|---|
| 1 | `PendingConsultantConfirmation` | در انتظار تایید مشاور |
| 2 | `ConsultantConfirmedPresent` | مشاور: بیمار آمده |
| 3 | `ConsultantConfirmedAbsent` | مشاور: بیمار نیامده |
| 4 | `SecretaryApproved` | منشی تایید کرده (+امتیاز) |
| 5 | `SecretaryRejected` | منشی رد کرده (-امتیاز) |

---

## API لیست رزروها (همه مشاورها)

`GET /api/Reservation/SecretaryReservations`

### Query params

| نام | نوع | پیش‌فرض | توضیح |
|---|---|---|---|
| `consultantProfileId` | `long?` | — | فیلتر یک مشاور؛ `null` = همه مشاورها |
| `from` | `DateTime?` | — | شروع بازه زمانی رزرو |
| `to` | `DateTime?` | — | پایان بازه زمانی رزرو |
| `attendanceConfirmationStatus` | `int?` | — | فیلتر وضعیت (۱ تا ۵) |
| `onlyWaitingForSecretaryReview` | `bool` | `false` | فقط صف بررسی منشی (وضعیت ۲ یا ۳) |
| `onlyDue` | `bool` | `false` | فقط رزروهایی که زمانشان رسیده (`reservationAt <= now`) |
| `patientName` | `string?` | — | جستجو در نام بیمار |
| `patientPhoneNumber` | `string?` | — | جستجو در موبایل/تلفن بیمار |
| `consultantName` | `string?` | — | جستجو در نام مشاور |
| `searchText` | `string?` | — | جستجوی ترکیبی در نام بیمار، موبایل و نام مشاور |
| `includeCanceled` | `bool` | `false` | نمایش رزروهای لغوشده |
| `pageNumber` | `int` | `1` | شماره صفحه |
| `pageSize` | `int` | `10` | اندازه صفحه (حداکثر ۱۰۰) |

### نمونه درخواست‌ها

**همه رزروها با صفحه‌بندی:**

```http
GET /api/Reservation/SecretaryReservations?pageNumber=1&pageSize=20
```

**صف بررسی منشی (اولویت بالا):**

```http
GET /api/Reservation/SecretaryReservations?onlyWaitingForSecretaryReview=true&onlyDue=true
```

**جستجوی ترکیبی:**

```http
GET /api/Reservation/SecretaryReservations?searchText=0912
```

**فیلتر مشاور + بازه تاریخ:**

```http
GET /api/Reservation/SecretaryReservations?consultantProfileId=49&from=2026-07-01T00:00:00&to=2026-07-31T23:59:59
```

**فیلتر وضعیت انجام‌شده:**

```http
GET /api/Reservation/SecretaryReservations?attendanceConfirmationStatus=4
```

### Response item

```json
{
  "id": 123,
  "leadAssignmentId": 10,
  "consultantProfileId": 49,
  "consultantUserId": "guid",
  "consultantFullName": "Sara Mohammadi",
  "patientUserId": null,
  "requiresPatientProfile": true,
  "reservationAt": "2026-07-02T14:30:00",
  "patientName": "Ali Ahmadi",
  "patientPhoneNumber": "09120000000",
  "secondaryPhoneNumber": "02100000000",
  "patientCity": "Tehran",
  "patientRegion": "West",
  "businessName": "Instagram Campaign",
  "attendanceProbabilityPercent": 80,
  "attendanceConfirmationStatus": 2,
  "consultantAttendanceConfirmedAt": "2026-07-02T14:45:00Z",
  "consultantSaysPatientAttended": true,
  "consultantAttendanceNote": "بیمار مراجعه کرد",
  "isWaitingForSecretaryReview": true,
  "isReservationDue": true,
  "secretaryReviewedAt": null,
  "secretaryUserId": null,
  "secretaryApprovedConsultantConfirmation": null,
  "secretaryReviewNote": null,
  "isAttendanceScoreApplied": false,
  "attendanceScoreValue": null,
  "attendanceScoreAppliedAt": null,
  "description": "توضیحات رزرو",
  "isCanceled": false
}
```

---

## API تکمیل پرونده بیمار

`POST /api/Reservation/CompletePatientProfile`

### Request body

```json
{
  "reservationId": 123,
  "firstName": "Ali",
  "lastName": "Ahmadi",
  "phoneNumber": "09120000000",
  "passwordHash": "123456",
  "avatarImageName": null,
  "gender": 1,
  "birthDate": "1995-01-01T00:00:00",
  "nationalCode": "0012345678",
  "address": "تهران، خیابان مثال",
  "emergencyPhoneNumber": "09121111111",
  "insuranceName": "تامین اجتماعی",
  "notes": "توضیحات"
}
```

### Response success

```json
{
  "isSuccess": true,
  "message": "پرونده بیمار برای رزرو با موفقیت تشکیل شد",
  "data": {
    "reservationId": 123,
    "patientUserId": "guid",
    "patientProfileId": 45,
    "leadAssignmentId": 10,
    "consultantProfileId": 49,
    "reservationAt": "2026-07-02T14:30:00",
    "patientName": "Ali Ahmadi",
    "patientPhoneNumber": "09120000000",
    "isCompleteProfile": true,
    "roleName": "Patient"
  }
}
```

---

## API بررسی تایید مشاور و اعمال امتیاز

`POST /api/Reservation/ReviewAttendance`

### Request body — تایید اظهار مشاور (بیمار آمده)

```json
{
  "reservationId": 123,
  "secretaryUserId": "guid",
  "approved": true,
  "note": "بیمار در کلینیک حضور داشت"
}
```

### Request body — رد اظهار مشاور (مشاور گفته آمده، منشی می‌گوید نیامده)

```json
{
  "reservationId": 123,
  "secretaryUserId": "guid",
  "approved": false,
  "note": "بیمار در کلینیک دیده نشد"
}
```

### منطق امتیاز

| عمل منشی | امتیاز | وضعیت نهایی |
|---|---|---|
| `approved = true` | **+10** | `SecretaryApproved` (4) |
| `approved = false` | **-10** | `SecretaryRejected` (5) |

امتیاز فقط **یک‌بار** برای هر رزرو اعمال می‌شود.

### Response success

```json
{
  "isSuccess": true,
  "message": "بررسی منشی ثبت و امتیاز مشاور اعمال شد"
}
```

### خطاهای مهم

| پیام | علت |
|---|---|
| `رزرو یافت نشد` | `reservationId` اشتباه |
| `ابتدا مشاور باید حضور یا عدم حضور بیمار را تایید کند` | وضعیت هنوز ۱ است |
| `امتیاز این بررسی قبلا اعمال شده است` | بررسی تکراری |
| `پروفایل مشاور یافت نشد` | مشکل داده |

---

## پیشنهاد UI (منشی)

### فیلترها و جستجو

| کنترل UI | Query param |
|---|---|
| جستجوی آزاد (نام بیمار / موبایل / مشاور) | `searchText` |
| فیلتر مشاور (dropdown) | `consultantProfileId` |
| بازه تاریخ | `from` + `to` |
| وضعیت | `attendanceConfirmationStatus` |
| فقط صف بررسی | `onlyWaitingForSecretaryReview=true` |
| فقط زمان رسیده | `onlyDue=true` |

### ستون‌های جدول

زمان رزرو، نام بیمار، موبایل، نام مشاور، احتمال حضور، وضعیت تایید، وضعیت پرونده بیمار، امتیاز اعمال‌شده.

### منطق دکمه‌ها

```typescript
const showCompleteProfileButton = (item) => item.requiresPatientProfile;

const showReviewButtons = (item) =>
  item.isWaitingForSecretaryReview &&
  item.isReservationDue &&
  !item.isCanceled;

const showScoreBadge = (item) =>
  item.isAttendanceScoreApplied && item.attendanceScoreValue != null;
```

| شرط | دکمه |
|---|---|
| `requiresPatientProfile=true` | «تکمیل پرونده» |
| `isWaitingForSecretaryReview=true` | «تایید اظهار مشاور» / «رد اظهار مشاور» |
| `attendanceConfirmationStatus` برابر ۴ یا ۵ | فقط نمایش وضعیت نهایی |

### تب‌های پیشنهادی

1. **صف بررسی** — `onlyWaitingForSecretaryReview=true&onlyDue=true`
2. **همه رزروها** — بدون فیلتر وضعیت
3. **انجام‌شده** — `attendanceConfirmationStatus=4` یا `=5`

### انتخاب مشاور برای dropdown

از API مشاوران موجود استفاده کنید:

```http
GET /api/Consultant/GetConsultants?pageNumber=1&pageSize=100
```

---

## مستند مرتبط

- لیست رزرو و تایید حضور مشاور: `docs/frontend-consultant-reservation-attendance-fa.md`
