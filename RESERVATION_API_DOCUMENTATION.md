# Reservation API Documentation

این سند APIهای سیستم رزرو را توضیح می‌دهد.

## قوانین اصلی

- رزرو فقط برای لیدهایی مجاز است که به همان مشاور اختصاص داده شده باشند.
- لید باید گزارش تماس ثبت‌شده داشته باشد و نتیجه تماس آن `Contacted = 1` یا `Converted = 2` باشد.
- هر مشاور می‌تواند در یک زمان دقیق حداکثر برای ۱۰ بیمار رزرو فعال داشته باشد.
- برای هر لید فقط یک رزرو فعال قابل ثبت است.
- تایید حضور مشاور فقط بعد از رسیدن `reservationAt` ممکن است.
- منشی فقط بعد از تایید مشاور می‌تواند بررسی کند و امتیاز اعمال شود.

## Enum وضعیت تایید حضور

| مقدار | نام |
|---:|---|
| 1 | `PendingConsultantConfirmation` |
| 2 | `ConsultantConfirmedPresent` |
| 3 | `ConsultantConfirmedAbsent` |
| 4 | `SecretaryApproved` |
| 5 | `SecretaryRejected` |

---

## POST `/api/Reservation`

ثبت رزرو برای یک لید تماس موفق.

### Request body

```json
{
  "leadAssignmentId": 12,
  "consultantProfileId": 3,
  "reservationAt": "2026-06-20T10:30:00",
  "description": "توضیحات اختیاری"
}
```

---

## GET `/api/Reservation/GetConsultantReservations`

لیست رزروهای یک مشاور (همه، انجام‌شده، در انتظار تایید).

### Query parameters

| Name | Type | Description |
|---|---|---|
| `consultantProfileId` | long | required |
| `from` | DateTime? | start of date range |
| `to` | DateTime? | end of date range |
| `attendanceConfirmationStatus` | int? | filter by status 1-5 |
| `onlyDueForConsultantConfirmation` | bool | only due, unconfirmed |
| `onlySecretaryReviewed` | bool | only completed (status 4 or 5) |
| `patientName` | string? | search patient name |
| `patientPhoneNumber` | string? | search patient phone |
| `includeCanceled` | bool | default false |
| `pageNumber` | int | default 1 |
| `pageSize` | int | default 10, max 100 |

---

## GET `/api/Reservation/DueConfirmations`

رزروهایی که زمانشان رسیده و دکمه تایید حضور مشاور باید فعال شود.

### Query parameters

| Name | Type | Description |
|---|---|---|
| `consultantProfileId` | long | required |
| `now` | DateTime? | optional override for testing |

---

## GET `/api/Reservation/SecretaryReservations`

لیست رزروها برای داشبورد منشی (همه مشاورها یا فیلتر یک مشاور).

### Query parameters

| Name | Type | Description |
|---|---|---|
| `consultantProfileId` | long? | null = all consultants |
| `from` | DateTime? | date range start |
| `to` | DateTime? | date range end |
| `attendanceConfirmationStatus` | int? | status filter |
| `onlyWaitingForSecretaryReview` | bool | review queue (status 2 or 3) |
| `onlyDue` | bool | reservation time reached |
| `patientName` | string? | search |
| `patientPhoneNumber` | string? | search |
| `consultantName` | string? | search |
| `searchText` | string? | combined search |
| `includeCanceled` | bool | default false |
| `pageNumber` | int | default 1 |
| `pageSize` | int | default 10, max 100 |

---

## POST `/api/Reservation/ConfirmAttendance`

مشاور اعلام می‌کند بیمار آمده یا نیامده.

### Request body

```json
{
  "reservationId": 123,
  "consultantProfileId": 49,
  "patientAttended": true,
  "note": "بیمار مراجعه کرد"
}
```

---

## POST `/api/Reservation/ReviewAttendance`

منشی اظهار مشاور را تایید/رد می‌کند و امتیاز اعمال می‌شود.

### Request body

```json
{
  "reservationId": 123,
  "secretaryUserId": "guid",
  "approved": true,
  "note": "تایید شد"
}
```

### Score rules

- `approved = true` → +10 points
- `approved = false` → -10 points
- Score applied only once per reservation

---

## POST `/api/Reservation/CompletePatientProfile`

منشی پرونده بیمار را برای رزرو تکمیل می‌کند.

---

## Frontend docs

- **مستند کامل پیاده‌سازی (اصلی):** `docs/frontend-reservation-attendance-implementation-fa.md`
- Consultant (خلاصه): `docs/frontend-consultant-reservation-attendance-fa.md`
- Secretary (خلاصه): `docs/frontend-secretary-reservation-workflow-fa.md`
