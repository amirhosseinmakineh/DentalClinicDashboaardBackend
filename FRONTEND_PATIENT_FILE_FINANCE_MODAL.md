# داکیومنت فرانت‌اند نمایش کامل اطلاعات مالی در لیست پرونده بیماران

## هدف و محل نمایش

این قابلیت مخصوص نقش **منشی** و صفحه «لیست پرونده‌های بیماران» است. API فهرست و جزئیات پرونده اکنون اطلاعات مالی کامل همان بیمار را نیز برمی‌گرداند.

- در هر ردیف/کارت پرونده، اکشن واضح «اطلاعات مالی» قرار دهید.
- با کلیک روی اکشن، تمام اطلاعات مالی در **Modal** نمایش داده شود و کاربر از صفحه فهرست خارج نشود.
- اگر `finance === null` بود، اکشن می‌تواند فعال بماند و Modal با Empty State و متن دقیق **«اطلاعات مالی ندارد»** نمایش داده شود.
- اگر `finance` مقدار داشت، خلاصه، تمام پرونده‌های مالی، چک‌ها، سفته‌ها، بدهی‌ها و تراکنش‌ها باید کامل نمایش داده شوند؛ صرفاً نمایش چند عدد خلاصه کافی نیست.

## دسترسی و API

```http
Authorization: Bearer <access-token>
```

Endpointهای موجود زیر برای نقش `Secretary` هستند:

```http
GET /api/secretary/patient-files?page=1&pageSize=20&search=
GET /api/secretary/patient-files/{id}
```

پاسخ فهرست:

```ts
interface PatientFilePageResponse {
  items: PatientFile[];
  page: number;
  pageSize: number;
  totalCount: number;
}
```

> اطلاعات مالی داخل هر آیتم `finance` قرار دارد؛ بنابراین برای بازکردن Modal نیازی به فراخوانی چند API مالی نیست. در صورت نیاز به تازه‌ترین داده در زمان بازشدن Modal، `GET /api/secretary/patient-files/{id}` را فراخوانی و Modal را با پاسخ جدید به‌روزرسانی کنید.

## قرارداد کامل TypeScript

```ts
type AgreementType = 1 | 2;       // 1: پیش‌پرداخت، 2: بیعانه
type CaseStatus = 1 | 2 | 3;      // 1: فعال، 2: تکمیل، 3: لغوشده
type CommitmentStatus = 1 | 2 | 3 | 4; // درانتظار، وصول/پرداخت، برگشتی/پرداخت‌نشده، لغوشده
type DebtStatus = 1 | 2 | 3;      // پرداخت‌نشده، پرداخت‌شده، لغوشده
type SourceType = 1 | 2;          // 1: چک، 2: سفته

interface PatientFile {
  id: number;
  patientId: number | null; // شناسه مرجع پرونده؛ برای APIهای مالی استفاده نشود
  fileNumber: number;
  firstName: string;
  lastName: string;
  phoneNumber: string;
  sourceType: number;
  createdAt: string;
  finance: PatientFinance | null;
}

interface PatientFinance {
  financialPatientId: number; // شناسه صحیح بیمار در ماژول حسابداری
  totalTreatmentAmount: number;
  totalPaidAmount: number;
  remainingAmount: number;
  totalDebtAmount: number;
  activeFinancialCasesCount: number;
  unpaidChequesCount: number;
  unpaidPromissoryNotesCount: number;
  cases: FinancialCase[];
}

interface FinancialCase {
  id: number;
  serviceId: number;
  serviceName: string;
  totalAmount: number;
  totalPaidAmount: number;
  remainingAmount: number;
  totalDebtAmount: number;
  agreementType: AgreementType;
  status: CaseStatus;
  createdAt: string;
  cheques: Cheque[];
  promissoryNotes: PromissoryNote[];
  debts: Debt[];
  transactions: Transaction[];
}

interface Cheque {
  id: number;
  amount: number;
  sayadNumber: string;
  ownerName: string;
  dueDate: string;
  status: CommitmentStatus;
}
interface PromissoryNote {
  id: number;
  serialNumber: string;
  amount: number;
  dueDate: string;
  status: CommitmentStatus;
}
interface Debt {
  id: number;
  amount: number;
  sourceType: SourceType;
  sourceId: number;
  dueDate: string;
  status: DebtStatus;
}
interface Transaction {
  id: number;
  amount: number;
  type: 1; // پرداخت
  sourceType: SourceType;
  sourceId: number;
  createdAt: string;
}
```

## نمونه پاسخ پرونده دارای اطلاعات مالی

```json
{
  "id": 31,
  "patientId": 112,
  "fileNumber": 10031,
  "firstName": "سارا",
  "lastName": "احمدی",
  "phoneNumber": "09121234567",
  "sourceType": 1,
  "createdAt": "2026-08-30T08:10:00Z",
  "finance": {
    "financialPatientId": 44,
    "totalTreatmentAmount": 90000000,
    "totalPaidAmount": 30000000,
    "remainingAmount": 60000000,
    "totalDebtAmount": 20000000,
    "activeFinancialCasesCount": 1,
    "unpaidChequesCount": 1,
    "unpaidPromissoryNotesCount": 0,
    "cases": [
      {
        "id": 78,
        "serviceId": 1,
        "serviceName": "Implant",
        "totalAmount": 90000000,
        "totalPaidAmount": 30000000,
        "remainingAmount": 60000000,
        "totalDebtAmount": 20000000,
        "agreementType": 1,
        "status": 1,
        "createdAt": "2026-08-29T10:00:00Z",
        "cheques": [{ "id": 9, "amount": 20000000, "sayadNumber": "1234567890123456", "ownerName": "علی احمدی", "dueDate": "2026-09-20T00:00:00Z", "status": 3 }],
        "promissoryNotes": [],
        "debts": [{ "id": 4, "amount": 20000000, "sourceType": 1, "sourceId": 9, "dueDate": "2026-09-20T00:00:00Z", "status": 1 }],
        "transactions": [{ "id": 18, "amount": 30000000, "type": 1, "sourceType": 1, "sourceId": 7, "createdAt": "2026-08-29T11:00:00Z" }]
      }
    ]
  }
}
```

پرونده بدون سابقه مالی دقیقاً `"finance": null` دارد. خالی‌بودن آرایه `cases` را معادل نداشتن مالی فرض نکنید؛ معیار اصلی `finance === null` است.

## طراحی الزامی Modal

### Header

- عنوان: «اطلاعات مالی {نام و نام خانوادگی}»
- زیرعنوان: شماره پرونده و موبایل بیمار
- دکمه بستن با پشتیبانی Escape؛ در موبایل Modal به صورت Full-screen نمایش داده شود.

### حالت بدون اطلاعات مالی

در بدنه Modal آیکن Empty State، متن **«اطلاعات مالی ندارد»** و دکمه «بستن» نمایش داده شود. مقدار صفر یا متن ساختگی به‌جای این پیام نمایش ندهید.

### حالت دارای اطلاعات مالی

1. کارت‌های خلاصه: مبلغ کل درمان، پرداخت‌شده، مانده، بدهی، تعداد پرونده فعال، چک برگشتی/پرداخت‌نشده و سفته پرداخت‌نشده.
2. هر عضو `cases` در Accordion مستقل با نام خدمت، وضعیت، نوع توافق، تاریخ ایجاد و چهار مبلغ پرونده نمایش داده شود.
3. داخل هر Accordion چهار Tab یا Section مستقل: «چک‌ها»، «سفته‌ها»، «بدهی‌ها»، «تراکنش‌ها»؛ هیچ آیتمی truncate یا حذف نشود.
4. برای آرایه خالی هر بخش Empty State همان بخش نمایش داده شود، مثلاً «چکی ثبت نشده است».
5. `sayadNumber` و `serialNumber` به صورت متن قابل کپی نمایش داده شوند و به عدد تبدیل نشوند.
6. مبلغ‌ها با جداکننده هزارگان، واحد ثابت پروژه (ریال/تومان) و بدون تبدیل ناخواسته واحد نمایش داده شوند.
7. تاریخ UTC با timezone و تقویم استاندارد پروژه نمایش داده شود؛ مقدار خام ISO در UI نشان داده نشود.

## نگاشت متن enumها

| فیلد | مقدار | برچسب فارسی |
|---|---:|---|
| `agreementType` | 1 | پیش‌پرداخت |
|  | 2 | بیعانه |
| `case.status` | 1 | فعال |
|  | 2 | تکمیل‌شده |
|  | 3 | لغوشده |
| چک/سفته `status` | 1 | در انتظار |
|  | 2 | وصول/پرداخت‌شده |
|  | 3 | برگشتی/پرداخت‌نشده |
|  | 4 | لغوشده |
| بدهی `status` | 1 | پرداخت‌نشده |
|  | 2 | پرداخت‌شده |
|  | 3 | لغوشده |
| `sourceType` | 1 | چک |
|  | 2 | سفته |

## State و رفتار پیشنهادی

- stateهای `closed | loading | success | empty | error` را صریح مدیریت کنید.
- هنگام بازشدن، داده موجود ردیف را فوراً نمایش دهید؛ اگر جزئیات مجدد fetch می‌شود، Skeleton غیرمسدودکننده نشان دهید.
- از `financialPatientId` برای هر درخواست حسابداری بعدی استفاده کنید، نه `patientId` پرونده.
- در خطای دریافت جزئیات، Modal باز بماند و Retry نمایش داده شود.
- با تغییر صفحه/فیلتر، Modal باز قبلی بسته و state پاک شود.
- اعداد شناسه Backend از نوع `Int64` هستند؛ در صورت احتمال عبور از safe integer جاوااسکریپت، آن‌ها را در لایه API به string تبدیل کنید.

## چک‌لیست پذیرش فرانت‌اند

- [ ] اکشن مالی در تمام ردیف‌ها/کارت‌های پرونده برای منشی وجود دارد.
- [ ] تمام اطلاعات فقط در Modal و بدون خروج از فهرست نمایش داده می‌شود.
- [ ] `finance === null` متن دقیق «اطلاعات مالی ندارد» را نشان می‌دهد.
- [ ] خلاصه مالی و تمام پرونده‌های مالی نمایش داده می‌شوند.
- [ ] تمام چک‌ها، سفته‌ها، بدهی‌ها و تراکنش‌ها قابل مشاهده‌اند.
- [ ] وضعیت‌ها ترجمه شده و فقط با رنگ منتقل نمی‌شوند.
- [ ] Loading، Error، Retry و Empty State پیاده‌سازی شده‌اند.
- [ ] Modal در موبایل قابل استفاده و محتوای طولانی آن scrollable است.
