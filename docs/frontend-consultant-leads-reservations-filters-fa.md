# راهنمای فرانت‌اند فیلتر لید و رزرو داشبورد مشاور

این تغییر، فیلترهای فیلدهای اصلی را به دو لیست صفحه‌بندی‌شده‌ی داشبورد مشاور اضافه می‌کند. فیلترها در دیتابیس و **پیش از** محاسبه‌ی `totalCount` و اعمال صفحه‌بندی اجرا می‌شوند؛ بنابراین بعد از تغییر هر فیلتر، فرانت باید `pageNumber` را به `1` برگرداند.

## فیلتر لیدها

### API

```http
GET /api/Consultant/GetLeads
```

پارامترهای جدید (همگی اختیاری):

| پارامتر | نوع | رفتار |
|---|---|---|
| `searchText` | `string` | جست‌وجوی بخشی در نام، شماره اصلی و شماره دوم |
| `userName` | `string` | جست‌وجوی بخشی در نام لید |
| `phoneNumber` | `string` | جست‌وجوی بخشی در شماره اصلی یا شماره دوم |
| `patientCity` | `string` | جست‌وجوی بخشی در شهر |

پارامترهای قبلی مثل `profileId`، `leadAssignmentState`، `leadAssignmentType`، `hasSubmittedReport`، `date`، `from`، `to`، `pageNumber` و `pageSize` بدون تغییر قابل استفاده‌اند. فاصله‌ی ابتدا و انتهای ورودی‌های متنی در بک‌اند حذف می‌شود و مقدار خالی نادیده گرفته می‌شود. ترکیب چند پارامتر با منطق **AND** است؛ فقط داخل `searchText` و تطبیق دو شماره منطق **OR** به‌کار می‌رود.

نمونه:

```http
GET /api/Consultant/GetLeads?profileId=59&searchText=0912&patientCity=تهران&leadAssignmentState=Assigned&pageNumber=1&pageSize=10
```

## فیلتر رزروها

### API

```http
GET /api/Reservation/GetConsultantReservations
```

پارامترهای جدید (همگی اختیاری):

| پارامتر | نوع | رفتار |
|---|---|---|
| `searchText` | `string` | جست‌وجوی بخشی در نام بیمار، شماره اصلی و شماره دوم |
| `patientName` | `string` | جست‌وجوی بخشی در نام بیمار |
| `patientPhoneNumber` | `string` | جست‌وجوی بخشی در شماره اصلی یا شماره دوم |
| `patientCity` | `string` | جست‌وجوی بخشی در شهر بیمار |
| `attendanceConfirmationStatus` | enum | تطبیق دقیق وضعیت تأیید حضور |

مقادیر enum وضعیت حضور:

| نام | مقدار عددی |
|---|---:|
| `PendingConsultantConfirmation` | 1 |
| `ConsultantConfirmedPresent` | 2 |
| `ConsultantConfirmedAbsent` | 3 |
| `SecretaryApproved` | 4 |
| `SecretaryRejected` | 5 |

پارامترهای قبلی `consultantProfileId`، `from`، `to`، `includeCanceled`، `onlySecretaryReviewed`، `pageNumber` و `pageSize` همچنان معتبرند. به‌صورت پیش‌فرض رزروهای لغوشده نمایش داده نمی‌شوند؛ برای جست‌وجو میان آن‌ها `includeCanceled=true` ارسال شود.

نمونه:

```http
GET /api/Reservation/GetConsultantReservations?consultantProfileId=59&patientName=علی&attendanceConfirmationStatus=ConsultantConfirmedPresent&pageNumber=1&pageSize=10
```

## پیشنهاد پیاده‌سازی UI

1. برای هر تب یک ورودی جست‌وجوی سریع بسازید و مقدار آن را به `searchText` بفرستید.
2. فیلترهای جزئی نام، تلفن و شهر را در بخش «فیلترهای بیشتر» قرار دهید.
3. برای رزروها یک select وضعیت حضور با مقدار خالی «همه» اضافه کنید.
4. ورودی جست‌وجوی سریع را با debounce حدود ۳۰۰ تا ۵۰۰ میلی‌ثانیه ارسال کنید.
5. با هر تغییر فیلتر، صفحه را روی ۱ بگذارید و پاسخ قبلی را با پاسخ جدید جایگزین کنید.
6. پارامترهای خالی را اصلاً در query string نفرستید.

نمونه‌ی TypeScript:

```typescript
loadLeads(): void {
  this.consultantApi.getLeads({
    profileId: this.profileId,
    searchText: this.leadSearch || undefined,
    userName: this.leadName || undefined,
    phoneNumber: this.leadPhone || undefined,
    patientCity: this.leadCity || undefined,
    pageNumber: this.leadPageNumber,
    pageSize: this.leadPageSize,
  }).subscribe(result => this.leads = result);
}

loadReservations(): void {
  this.reservationApi.getConsultantReservations({
    consultantProfileId: this.profileId,
    searchText: this.reservationSearch || undefined,
    patientName: this.patientName || undefined,
    patientPhoneNumber: this.patientPhone || undefined,
    patientCity: this.patientCity || undefined,
    attendanceConfirmationStatus: this.attendanceStatus || undefined,
    pageNumber: this.reservationPageNumber,
    pageSize: this.reservationPageSize,
  }).subscribe(result => this.reservations = result);
}
```

## نکات سازگاری و استقرار

- این تغییر migration دیتابیس ندارد.
- قرارداد پاسخ API تغییر نکرده است.
- کلاینت‌های قبلی بدون ارسال پارامترهای جدید همان رفتار قبلی را خواهند داشت.
- نام enum یا مقدار عددی آن توسط ASP.NET Core قابل ارسال است؛ استفاده از نام enum در فرانت خواناتر است.
