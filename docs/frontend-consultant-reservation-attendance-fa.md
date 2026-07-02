# مستند فرانت‌اند: لیست رزروها و تایید حضور بیمار (مشاور)

این مستند برای پیاده‌سازی بخش رزرو و تایید حضور در داشبورد مشاور آماده شده است.

## جریان کلی

1. مشاور بعد از تماس موفق، رزرو بیمار را ثبت می‌کند (`POST /api/Reservation`).
2. تا قبل از رسیدن `reservationAt`، دکمه «ثبت حضور» غیرفعال است.
3. بعد از رسیدن روز و ساعت رزرو، رزرو در لیست `DueConfirmations` ظاهر می‌شود و دکمه فعال می‌شود.
4. مشاور اعلام می‌کند بیمار آمده (`patientAttended=true`) یا نیامده (`patientAttended=false`).
5. رزرو وارد صف بررسی منشی می‌شود.
6. بعد از بررسی منشی، رزرو در لیست «انجام‌شده» (`onlySecretaryReviewed=true`) نمایش داده می‌شود.

## Enum وضعیت تایید حضور

`ReservationAttendanceConfirmationStatus`:

| مقدار | نام | معنی |
|---:|---|---|
| 1 | `PendingConsultantConfirmation` | در انتظار تایید مشاور |
| 2 | `ConsultantConfirmedPresent` | مشاور اعلام کرده بیمار آمده است |
| 3 | `ConsultantConfirmedAbsent` | مشاور اعلام کرده بیمار نیامده است |
| 4 | `SecretaryApproved` | منشی تایید کرده و امتیاز اعمال شده است |
| 5 | `SecretaryRejected` | منشی رد کرده و جریمه اعمال شده است |

---

## API لیست همه رزروهای مشاور

`GET /api/Reservation/GetConsultantReservations`

### Query params

| نام | نوع | پیش‌فرض | توضیح |
|---|---|---|---|
| `consultantProfileId` | `long` | الزامی | شناسه پروفایل مشاور |
| `from` | `DateTime?` | — | شروع بازه زمانی رزرو |
| `to` | `DateTime?` | — | پایان بازه زمانی رزرو |
| `attendanceConfirmationStatus` | `int?` | — | فیلتر وضعیت تایید حضور (۱ تا ۵) |
| `onlyDueForConsultantConfirmation` | `bool` | `false` | فقط رزروهایی که زمانشان رسیده و هنوز تایید نشده‌اند |
| `onlySecretaryReviewed` | `bool` | `false` | فقط رزروهای انجام‌شده (وضعیت ۴ یا ۵) |
| `patientName` | `string?` | — | جستجو در نام بیمار |
| `patientPhoneNumber` | `string?` | — | جستجو در موبایل/تلفن بیمار |
| `includeCanceled` | `bool` | `false` | نمایش رزروهای لغوشده |
| `pageNumber` | `int` | `1` | شماره صفحه |
| `pageSize` | `int` | `10` | اندازه صفحه (حداکثر ۱۰۰) |

### نمونه درخواست‌ها

**لیست رزروهای انجام‌شده (بررسی‌شده توسط منشی):**

```http
GET /api/Reservation/GetConsultantReservations?consultantProfileId=49&onlySecretaryReviewed=true&pageNumber=1&pageSize=20
```

**لیست رزروهای در انتظار تایید حضور:**

```http
GET /api/Reservation/GetConsultantReservations?consultantProfileId=49&onlyDueForConsultantConfirmation=true
```

**فیلتر بر اساس وضعیت:**

```http
GET /api/Reservation/GetConsultantReservations?consultantProfileId=49&attendanceConfirmationStatus=4
```

### Response

```json
{
  "items": [
    {
      "id": 123,
      "leadAssignmentId": 10,
      "consultantProfileId": 49,
      "patientUserId": "guid-or-null",
      "requiresPatientProfile": false,
      "reservationAt": "2026-07-02T14:30:00",
      "patientName": "Ali Ahmadi",
      "patientPhoneNumber": "09120000000",
      "secondaryPhoneNumber": "02100000000",
      "patientCity": "Tehran",
      "patientRegion": "West",
      "businessName": "Instagram",
      "attendanceProbabilityPercent": 80,
      "attendanceConfirmationStatus": 4,
      "consultantAttendanceConfirmedAt": "2026-07-02T14:45:00Z",
      "consultantSaysPatientAttended": true,
      "consultantAttendanceNote": "بیمار مراجعه کرد",
      "secretaryReviewedAt": "2026-07-02T15:00:00Z",
      "secretaryUserId": "guid",
      "secretaryApprovedConsultantConfirmation": true,
      "secretaryReviewNote": "تایید شد",
      "isAttendanceScoreApplied": true,
      "attendanceScoreValue": 10,
      "attendanceScoreAppliedAt": "2026-07-02T15:00:00Z",
      "isDueForConsultantConfirmation": false,
      "description": "توضیحات رزرو",
      "isCanceled": false
    }
  ],
  "totalCount": 1,
  "pageNumber": 1,
  "pageSize": 20,
  "totalPages": 1
}
```

---

## API رزروهای آماده تایید حضور (دکمه فعال)

`GET /api/Reservation/DueConfirmations`

### Query params

| نام | نوع | توضیح |
|---|---|---|
| `consultantProfileId` | `long` | الزامی |
| `now` | `DateTime?` | اختیاری؛ برای تست (پیش‌فرض: زمان سرور) |

### نمونه

```http
GET /api/Reservation/DueConfirmations?consultantProfileId=49
```

فقط رزروهایی برمی‌گردد که:
- لغو نشده باشند
- `reservationAt <= now`
- `attendanceConfirmationStatus == 1` (PendingConsultantConfirmation)

### Response

آرایه‌ای از `ReservationItemResponse` (بدون صفحه‌بندی). فیلد `isDueForConsultantConfirmation` همیشه `true` است.

---

## API ثبت حضور یا عدم حضور بیمار

`POST /api/Reservation/ConfirmAttendance`

### Request body

```json
{
  "reservationId": 123,
  "consultantProfileId": 49,
  "patientAttended": true,
  "note": "بیمار مراجعه کرد"
}
```

### Response success

```json
{
  "isSuccess": true,
  "message": "تایید حضور بیمار ثبت شد و در انتظار بررسی منشی است"
}
```

### خطاهای مهم

| پیام | علت |
|---|---|
| `رزرو برای این مشاور یافت نشد` | `reservationId` یا `consultantProfileId` اشتباه |
| `رزرو لغو شده قابل تایید حضور نیست` | رزرو کنسل شده |
| `تایید حضور فقط بعد از رسیدن روز و ساعت رزرو ممکن است` | هنوز زمان رزرو نرسیده |
| `تایید حضور این رزرو قبلا ثبت شده است` | مشاور قبلاً تایید زده |
| `این رزرو قبلا توسط منشی بررسی شده است` | فرآیند تمام شده |

---

## پیشنهاد UI (مشاور)

### تب‌ها

| تب | API | توضیح |
|---|---|---|
| در انتظار تایید | `DueConfirmations` یا `onlyDueForConsultantConfirmation=true` | دکمه «بیمار آمد» / «بیمار نیامد» فعال |
| همه رزروها | `GetConsultantReservations` | لیست کامل با فیلتر تاریخ |
| انجام‌شده | `onlySecretaryReviewed=true` | رزروهایی که منشی بررسی کرده |

### منطق فعال/غیرفعال بودن دکمه تایید حضور

```typescript
const canConfirmAttendance = (item: ReservationItemResponse) =>
  item.isDueForConsultantConfirmation && !item.isCanceled;
```

### بعد از ثبت تایید

1. رزرو را از لیست `DueConfirmations` حذف کنید.
2. وضعیت را به «در انتظار بررسی منشی» تغییر دهید.
3. لیست «انجام‌شده» را بعد از بررسی منشی رفرش کنید (`attendanceConfirmationStatus` برابر ۴ یا ۵).

### نمایش امتیاز

اگر `isAttendanceScoreApplied=true`:
- `attendanceScoreValue > 0` → نمایش «+۱۰ امتیاز»
- `attendanceScoreValue < 0` → نمایش «-۱۰ امتیاز»

---

## باگ رفع‌شده: آنلاین شدن مشاور

در `GET /api/Consultant/GetDashboardStatus` شمارش `pendingOfflineLeadCount` اصلاح شد.
لید آفلاینی فقط وقتی «در انتظار» است که `reportSubmittedAt == null` باشد.
اگر مشاور همه لیدها را گزارش داده ولی `canGoOnline=false` می‌بیند، بعد از deploy این نسخه مشکل برطرف می‌شود.
