# مستند فرانت: فلو رزرو و تشکیل پرونده بیمار توسط مشاور

## سناریوی جدید

1. مشاور برای لید تماس می‌گیرد.
2. مشاور گزارش تماس را ثبت می‌کند.
3. اگر `callResult` برابر یکی از مقادیر تماس موفق باشد، بک‌اند در پاسخ گزارش تماس مقدار `shouldOpenReservation: true` برمی‌گرداند و فرانت باید صفحه/فرم رزرو را باز کند.
4. مشاور برای همان `leadAssignmentId` رزرو ثبت می‌کند.
5. بعد از ثبت رزرو، اگر پاسخ رزرو مقدار `shouldOpenPatientProfileDialog: true` داشت، فرانت باید دیالوگ تشکیل پرونده بیمار را باز کند.
6. دیالوگ تشکیل پرونده، اطلاعات ثبت‌نام کاربر + فیلدهای پرونده بیمار را به بک‌اند می‌فرستد. بک‌اند کاربر را با نقش `Patient` و با شماره موبایل همان لید ایجاد/تکمیل می‌کند.

> نکته مهم: شماره موبایل بیمار از لید رزرو شده برداشته می‌شود و فرانت نباید شماره موبایل جداگانه برای این endpoint ارسال کند.

## تماس موفق چیست؟

برای باز شدن فرم رزرو، `callResult` باید یکی از مقادیر زیر باشد:

| مقدار عددی | نام enum |
|---:|---|
| `1` | `Contacted` |
| `2` | `Converted` |

## 1) ثبت گزارش تماس لید

### Endpoint

```http
POST /api/Consultant/SubmitLeadCallReport
```

### Request نمونه

```json
{
  "leadAssignmentId": 25,
  "consultantProfileId": 7,
  "callResult": 1,
  "reportDescription": "تماس موفق بود و بیمار برای رزرو موافقت کرد."
}
```

### Response فیلد جدید

```json
{
  "isSuccess": true,
  "data": {
    "leadAssignmentId": 25,
    "consultantProfileId": 7,
    "isReportSubmitted": true,
    "reportSubmittedAt": "2026-06-24T10:30:00",
    "leadAssignmentState": 3,
    "callResult": 1,
    "isConsultantOnline": true,
    "shouldOpenReservation": true
  },
  "message": "گزارش ثبت شد و شما به صورت خودکار آنلاین شدید"
}
```

### رفتار فرانت

- اگر `data.shouldOpenReservation === true` بود، فرم رزرو را برای همان `leadAssignmentId` و `consultantProfileId` باز کنید.
- اگر مقدار false بود، فرم رزرو باز نشود.

## 2) ثبت رزرو برای همان لید

### Endpoint

```http
POST /api/Reservation
```

### Request نمونه

```json
{
  "leadAssignmentId": 25,
  "consultantProfileId": 7,
  "reservationAt": "2026-06-25T12:00:00",
  "description": "رزرو اولیه بعد از تماس موفق"
}
```

### Response فیلدهای جدید

```json
{
  "isSuccess": true,
  "data": {
    "id": 101,
    "leadAssignmentId": 25,
    "consultantProfileId": 7,
    "reservationAt": "2026-06-25T12:00:00",
    "patientName": "نام بیمار از لید",
    "patientPhoneNumber": "09120000000",
    "patientUserId": null,
    "shouldOpenPatientProfileDialog": true
  },
  "message": "رزرو با موفقیت ثبت شد"
}
```

### رفتار فرانت

- رزرو فقط برای لید دارای گزارش تماس موفق پذیرفته می‌شود.
- بعد از موفقیت رزرو:
  - اگر `data.shouldOpenPatientProfileDialog === true` بود، دیالوگ تشکیل پرونده را باز کنید.
  - مقدار `data.id` را به عنوان `reservationId` به endpoint تشکیل پرونده ارسال کنید.
  - شماره موبایل را فقط برای نمایش از `patientPhoneNumber` نشان دهید؛ در request تشکیل پرونده ارسال نمی‌شود.

## 3) تشکیل پرونده بیمار بعد از رزرو

### Endpoint

```http
POST /api/Reservation/CompletePatientProfile
```

### Request

فیلدهای کاربری همان فیلدهای ثبت‌نام هستند، به‌جز اینکه `phoneNumber` ارسال نمی‌شود چون از لید رزرو شده خوانده می‌شود.

```json
{
  "reservationId": 101,
  "firstName": "علی",
  "lastName": "رضایی",
  "passwordHash": "123456",
  "avatarImageName": null,
  "gender": 1,
  "birthDate": "1995-03-21T00:00:00",
  "nationalCode": "0012345678",
  "address": "تهران، ...",
  "emergencyPhoneNumber": "09121111111",
  "insuranceName": "تامین اجتماعی",
  "notes": "توضیحات اولیه پرونده"
}
```

### فیلدها

| فیلد | نوع | الزامی | توضیح |
|---|---|---:|---|
| `reservationId` | `number` | بله | شناسه رزرو ایجاد شده در مرحله قبل |
| `firstName` | `string` | بله | نام بیمار |
| `lastName` | `string` | بله | نام خانوادگی بیمار |
| `passwordHash` | `string` | بله | رمز عبور خام مثل ثبت‌نام فعلی؛ بک‌اند هش می‌کند |
| `avatarImageName` | `string?` | خیر | نام تصویر آواتار |
| `gender` | `number` | بله | `1=Male`, `2=Female` |
| `birthDate` | `datetime` | بله | تاریخ تولد |
| `nationalCode` | `string` | بله | کد ملی پرونده بیمار |
| `address` | `string` | بله | آدرس پرونده بیمار |
| `emergencyPhoneNumber` | `string?` | خیر | شماره اضطراری |
| `insuranceName` | `string?` | خیر | نام بیمه |
| `notes` | `string?` | خیر | توضیحات پرونده |

### Response نمونه

```json
{
  "isSuccess": true,
  "data": {
    "userId": "7f7fb5f5-6a9d-4af3-a8b5-5c7f0d38a111",
    "patientProfileId": 55,
    "reservationId": 101,
    "leadAssignmentId": 25,
    "phoneNumber": "09120000000",
    "roleName": "Patient"
  },
  "message": "پرونده بیمار با موفقیت تشکیل شد"
}
```

## خطاهای مهم قابل نمایش

| مرحله | پیام |
|---|---|
| رزرو | `فقط لیدهای تماس موفق قابل رزرو هستند` |
| رزرو | `برای این بیمار قبلا رزرو فعال ثبت شده است` |
| تشکیل پرونده | `رزرو فعال یافت نشد` |
| تشکیل پرونده | `فقط رزرو لیدهای دارای تماس موفق قابل تشکیل پرونده است` |
| تشکیل پرونده | `برای این شماره موبایل قبلا پرونده بیمار تشکیل شده است` |
| تشکیل پرونده | `نام و نام خانوادگی بیمار الزامی است` |
| تشکیل پرونده | `رمز عبور بیمار الزامی است` |
| تشکیل پرونده | `کد ملی و آدرس بیمار الزامی است` |

## نکته دیتابیس/مایگریشن

در این تغییر هیچ migration جدیدی اضافه نشده است. از جداول موجود `Users`، `PatientProfiles`، `Roles/UserRoles`، `LeadAssignments` و `Reservations` استفاده شده است.
