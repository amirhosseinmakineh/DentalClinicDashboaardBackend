# مستند فرانت: رزرو مشاور و تشکیل پرونده بیمار

## فلو جدید

1. مشاور برای لید خود گزارش تماس ثبت می‌کند.
2. اگر نتیجه تماس مثبت باشد (`Contacted = 1` یا `Converted = 2`) پاسخ ثبت گزارش فیلد `shouldOpenReservationPage: true` برمی‌گرداند.
3. فرانت صفحه/فرم رزرو را برای همان `leadAssignmentId` و `consultantProfileId` باز می‌کند.
4. بعد از ثبت موفق رزرو، پاسخ رزرو فیلد `requiresPatientProfile: true` دارد؛ بنابراین فرانت باید دیالوگ تشکیل پرونده بیمار را باز کند.
5. دیالوگ تشکیل پرونده اطلاعات ثبت‌نام کاربر را به‌همراه فیلدهای پروفایل بیمار ارسال می‌کند. بک‌اند کاربر را با نقش `Patient` می‌سازد، پروفایل بیمار را ایجاد می‌کند و رزرو را به همان کاربر بیمار وصل می‌کند.

> نکته بک‌اند: برای این تغییر migration اضافه نشده است. اگر ستون/رابطه جدید را می‌خواهید در دیتابیس اعمال کنید، migration باید جداگانه توسط تیم شما ساخته شود.

## ثبت گزارش تماس لید

`POST /api/Consultant/SubmitLeadCallReport`

### فیلد مهم پاسخ

```json
{
  "data": {
    "leadAssignmentId": 12,
    "consultantProfileId": 3,
    "callResult": 1,
    "shouldOpenReservationPage": true
  }
}
```

- وقتی `shouldOpenReservationPage` برابر `true` است، فرانت باید فرم رزرو را باز کند.
- مقدار مثبت تماس در حال حاضر `Contacted = 1` و `Converted = 2` است.

## ثبت رزرو برای لید مثبت

`POST /api/Reservation`

### Request

```json
{
  "leadAssignmentId": 12,
  "consultantProfileId": 3,
  "reservationAt": "2026-06-25T10:30:00",
  "description": "توضیح اختیاری"
}
```

### Response موفق

```json
{
  "data": {
    "id": 45,
    "leadAssignmentId": 12,
    "consultantProfileId": 3,
    "patientUserId": null,
    "requiresPatientProfile": true,
    "reservationAt": "2026-06-25T10:30:00",
    "patientName": "نام لید",
    "patientPhoneNumber": "09123456789"
  },
  "message": "رزرو با موفقیت ثبت شد"
}
```

- اگر `requiresPatientProfile` برابر `true` بود، دیالوگ تشکیل پرونده را برای همین `reservationId` باز کنید.
- شماره موبایل دیالوگ باید همان `patientPhoneNumber` رزرو باشد؛ بک‌اند شماره متفاوت را رد می‌کند.

## تشکیل پرونده بیمار برای رزرو

`POST /api/Reservation/CompletePatientProfile`

### Request

```json
{
  "reservationId": 45,
  "firstName": "علی",
  "lastName": "رضایی",
  "phoneNumber": "09123456789",
  "passwordHash": "123456",
  "avatarImageName": null,
  "gender": 1,
  "birthDate": "1995-01-01T00:00:00",
  "nationalCode": "0012345678",
  "address": "تهران، ...",
  "emergencyPhoneNumber": "09120000000",
  "insuranceName": "تامین اجتماعی",
  "notes": "توضیحات پرونده"
}
```

### فیلدهای لازم

| فیلد | اجباری | توضیح |
| --- | --- | --- |
| `reservationId` | بله | شناسه رزرو موفق |
| `firstName` | بله | همان فیلد ثبت‌نام کاربر |
| `lastName` | بله | همان فیلد ثبت‌نام کاربر |
| `phoneNumber` | بله | باید با شماره لید رزرو یکی باشد |
| `passwordHash` | بله | مطابق معماری فعلی نام فیلد همین است؛ بک‌اند مقدار را هش می‌کند |
| `avatarImageName` | خیر | همان فیلد ثبت‌نام کاربر |
| `gender` | بله | `Male = 1`, `Female = 2` |
| `birthDate` | بله | تاریخ تولد بیمار |
| `nationalCode` | بله | کد ملی پرونده بیمار |
| `address` | بله | آدرس پرونده بیمار |
| `emergencyPhoneNumber` | خیر | شماره اضطراری |
| `insuranceName` | خیر | بیمه |
| `notes` | خیر | توضیحات پرونده |

### Response موفق

```json
{
  "data": {
    "reservationId": 45,
    "patientUserId": "1f0b75ef-0cc6-4c3d-9b18-3a5c0d78d120",
    "patientProfileId": 10,
    "leadAssignmentId": 12,
    "consultantProfileId": 3,
    "reservationAt": "2026-06-25T10:30:00",
    "patientName": "علی رضایی",
    "patientPhoneNumber": "09123456789",
    "isCompleteProfile": true,
    "roleName": "Patient"
  },
  "message": "پرونده بیمار برای رزرو با موفقیت تشکیل شد"
}
```

## خطاهای مهم قابل نمایش

- `رزرو فعال یافت نشد`
- `برای این رزرو قبلا پرونده بیمار تشکیل شده است`
- `شماره موبایل بیمار باید با شماره لید رزرو شده یکسان باشد`
- `کاربری با این شماره موبایل قبلاً ثبت شده است`
- `فقط لیدهای تماس موفق قابل رزرو هستند`
- `برای این بیمار قبلا رزرو فعال ثبت شده است`
