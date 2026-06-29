# مستند فرانت‌اند: رزرو بیمار، تکمیل پروفایل، تایید حضور و امتیاز مشاور

این مستند برای پیاده‌سازی داشبورد منشی و بخش تایید حضور مشاور آماده شده است.

## جریان کلی

1. مشاور بعد از تماس موفق، رزرو بیمار را ثبت می‌کند.
2. منشی می‌تواند برای همان رزرو پرونده بیمار را تکمیل کند. تکمیل پرونده شامل `Address` و `NationalCode` الزامی است.
3. داشبورد منشی لیست رزروها را همراه با زمان رزرو، بیمار، مشاور ثبت‌کننده رزرو و احتمال حضور بیمار نمایش می‌دهد.
4. در پنل مشاور، فقط بعد از رسیدن تاریخ و ساعت رزرو، رزرو در لیست `DueConfirmations` می‌آید و دکمه ثبت حضور فعال می‌شود.
5. مشاور حضور یا عدم حضور بیمار را ثبت می‌کند.
6. رزرو وارد صف بررسی منشی می‌شود.
7. اگر منشی تایید کند، امتیاز مثبت به مشاور اضافه می‌شود. اگر منشی تایید نکند، امتیاز منفی برای مشاور ثبت می‌شود.

## Enum وضعیت تایید حضور

`ReservationAttendanceConfirmationStatus`:

| مقدار | نام | معنی |
|---:|---|---|
| 1 | `PendingConsultantConfirmation` | در انتظار تایید مشاور |
| 2 | `ConsultantConfirmedPresent` | مشاور اعلام کرده بیمار آمده است |
| 3 | `ConsultantConfirmedAbsent` | مشاور اعلام کرده بیمار نیامده است |
| 4 | `SecretaryApproved` | منشی تایید کرده و امتیاز اعمال شده است |
| 5 | `SecretaryRejected` | منشی رد کرده و جریمه اعمال شده است |

## API تکمیل پرونده بیمار توسط منشی

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
    "consultantProfileId": 7,
    "reservationAt": "2026-06-29T14:30:00",
    "patientName": "Ali Ahmadi",
    "patientPhoneNumber": "09120000000",
    "isCompleteProfile": true,
    "roleName": "Patient"
  }
}
```

### خطاهای مهم

- `رزرو فعال یافت نشد`
- `برای این رزرو قبلا پرونده بیمار تشکیل شده است`
- `کد ملی بیمار الزامی است`
- `آدرس بیمار الزامی است`
- `شماره موبایل بیمار باید با شماره لید رزرو شده یکسان باشد`
- `کاربری با این شماره موبایل قبلاً ثبت شده است`

## API داشبورد منشی: لیست رزروها

`GET /api/Reservation/SecretaryReservations`

### Query params

| نام | نوع | توضیح |
|---|---|---|
| `consultantProfileId` | `long?` | فیلتر بر اساس مشاور |
| `from` | `DateTime?` | شروع بازه زمانی رزرو |
| `to` | `DateTime?` | پایان بازه زمانی رزرو |
| `attendanceConfirmationStatus` | `int?` | فیلتر وضعیت تایید حضور |
| `onlyWaitingForSecretaryReview` | `bool` | فقط رزروهایی که مشاور نظر داده و منتظر بررسی منشی هستند |
| `includeCanceled` | `bool` | نمایش رزروهای لغو شده |
| `pageNumber` | `int` | پیش‌فرض 1 |
| `pageSize` | `int` | پیش‌فرض 10، حداکثر 100 |

### Response item

```json
{
  "id": 123,
  "leadAssignmentId": 10,
  "consultantProfileId": 7,
  "consultantUserId": "guid",
  "consultantFullName": "Sara Mohammadi",
  "patientUserId": null,
  "requiresPatientProfile": true,
  "reservationAt": "2026-06-29T14:30:00",
  "patientName": "Ali Ahmadi",
  "patientPhoneNumber": "09120000000",
  "secondaryPhoneNumber": "02100000000",
  "patientCity": "Tehran",
  "patientRegion": "West",
  "businessName": "Instagram Campaign",
  "attendanceProbabilityPercent": 80,
  "attendanceConfirmationStatus": 2,
  "consultantAttendanceConfirmedAt": "2026-06-29T14:45:00Z",
  "consultantSaysPatientAttended": true,
  "consultantAttendanceNote": "بیمار مراجعه کرد",
  "isWaitingForSecretaryReview": true,
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

## API مشاور: رزروهایی که دکمه حضورشان فعال است

`GET /api/Reservation/DueConfirmations?consultantProfileId=7`

فقط رزروهایی برمی‌گردد که:

- لغو نشده باشند.
- زمان رزرو آن‌ها رسیده یا گذشته باشد.
- هنوز در وضعیت `PendingConsultantConfirmation` باشند.

در فرانت، دکمه ثبت حضور را برای این لیست فعال کنید.

## API مشاور: ثبت حضور یا عدم حضور بیمار

`POST /api/Reservation/ConfirmAttendance`

### Request body

```json
{
  "reservationId": 123,
  "consultantProfileId": 7,
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

- `رزرو برای این مشاور یافت نشد`
- `رزرو لغو شده قابل تایید حضور نیست`
- `تایید حضور فقط بعد از رسیدن روز و ساعت رزرو ممکن است`
- `این رزرو قبلا توسط منشی بررسی شده است`

## API منشی: بررسی تایید مشاور و اعمال امتیاز

`POST /api/Reservation/ReviewAttendance`

### Request body

```json
{
  "reservationId": 123,
  "secretaryUserId": "guid",
  "approved": true,
  "note": "اظهار مشاور توسط منشی تایید شد"
}
```

### منطق امتیاز

- اگر `approved = true` باشد: `+10` امتیاز برای مشاور ثبت می‌شود.
- اگر `approved = false` باشد: `-10` امتیاز برای مشاور ثبت می‌شود.
- امتیاز فقط یک‌بار برای هر رزرو اعمال می‌شود.

### Response success

```json
{
  "isSuccess": true,
  "message": "بررسی منشی ثبت و امتیاز مشاور اعمال شد"
}
```

### خطاهای مهم

- `رزرو یافت نشد`
- `ابتدا مشاور باید حضور یا عدم حضور بیمار را تایید کند`
- `امتیاز این بررسی قبلا اعمال شده است`
- `پروفایل مشاور یافت نشد`

## پیشنهاد UI

- ستون‌های اصلی داشبورد منشی: زمان رزرو، نام بیمار، موبایل بیمار، نام مشاور، احتمال حضور، وضعیت تایید حضور، وضعیت پرونده بیمار.
- برای صف بررسی منشی از `onlyWaitingForSecretaryReview=true` استفاده کنید.
- اگر `requiresPatientProfile=true` بود، دکمه «تکمیل پرونده» را نمایش دهید.
- اگر `isWaitingForSecretaryReview=true` بود، دکمه‌های «تایید اظهارنظر مشاور» و «رد اظهارنظر مشاور» را نمایش دهید.
