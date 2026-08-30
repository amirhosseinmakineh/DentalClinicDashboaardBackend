# سند کامل اتصال Frontend به ماژول مالی بیماران

این سند قرارداد فعلی Backend و راهنمای پیاده‌سازی رابط کاربری بخش حسابداری بیمار برای منشی است. هدف این است که Frontend بدون حدس‌زدن درباره وضعیت‌ها، محاسبات مالی یا گردش چک و سفته بتواند Feature را پیاده‌سازی کند.

> **Base URL:** تمام Routeها با `/api/secretary` شروع می‌شوند و به JWT معتبر نیاز دارند. شناسه منشی از Token خوانده می‌شود؛ آن را در Body نفرستید.

---

## 1. مفاهیم اصلی

هر بیمار برای هر خدمت می‌تواند یک یا چند **پرونده مالی مستقل** داشته باشد. تمام چک‌ها، سفته‌ها، بدهی‌ها و پرداخت‌های یک درمان زیر همان پرونده قرار می‌گیرند.

```text
Patient
└── Financial Case (Service + TotalAmount)
    ├── Cheques
    ├── Promissory Notes
    ├── Debts
    └── Confirmed Payment Transactions
```

- ثبت چک یا سفته **پرداخت محسوب نمی‌شود**.
- مبلغ فقط بعد از تغییر وضعیت چک/سفته به `Paid` یا تسویه Debt وارد `totalPaidAmount` می‌شود.
- `remainingAmount` و Summaryها را Frontend محاسبه نکند؛ مقدار Backend مرجع نهایی است.
- رکوردهای مالی حذف فیزیکی نمی‌شوند. حذف پرونده در UI به معنی Cancel کردن آن است.
- مبالغ JSON از نوع number هستند، ولی به دلیل دقت مالی در UI با کتابخانه/روش امن نمایش داده شوند و هیچ‌وقت از محاسبات float به عنوان مقدار نهایی استفاده نشود.

---

## 2. Enumها

### نوع توافق مالی (`PatientFinancialAgreementType`)

| مقدار | نام | رفتار |
|---:|---|---|
| 1 | `PrePayment` | هنگام ایجاد پرونده حداقل یک چک یا سفته اجباری است. |
| 2 | `Deposit` | ایجاد پرونده بدون چک و سفته مجاز است و تعهد بعداً اضافه می‌شود. |

### وضعیت پرونده (`PatientFinancialCaseStatus`)

| مقدار | نام | نمایش پیشنهادی |
|---:|---|---|
| 1 | `Active` | فعال |
| 2 | `Completed` | تسویه‌شده |
| 3 | `Cancelled` | لغوشده |

### وضعیت چک (`PatientChequeStatus`)

| مقدار | نام | نمایش پیشنهادی |
|---:|---|---|
| 1 | `Pending` | در انتظار تعیین وضعیت |
| 2 | `Paid` | وصول‌شده |
| 3 | `Unpaid` | برگشتی/پرداخت‌نشده |
| 4 | `Cancelled` | لغوشده |

### وضعیت سفته (`PatientPromissoryNoteStatus`)

مقادیر دقیقاً مشابه چک است: `Pending=1`, `Paid=2`, `Unpaid=3`, `Cancelled=4`.

### وضعیت بدهی (`PatientDebtStatus`)

| مقدار | نام |
|---:|---|
| 1 | `Unpaid` |
| 2 | `Paid` |
| 3 | `Cancelled` |

### نوع منبع بدهی/تراکنش

| مقدار | نام |
|---:|---|
| 1 | `Cheque` |
| 2 | `PromissoryNote` |

### نوع تراکنش

در Scope فعلی فقط `Payment=1` وجود دارد.

### خدمات

Backend فعلی خدمت را بر اساس Enum دریافت می‌کند:

| `serviceId` | نام Backend | عنوان پیشنهادی |
|---:|---|---|
| 1 | `Composite` | کامپوزیت |
| 2 | `Implant` | ایمپلنت |
| 3 | `Laminate` | لمینت |

---

## 3. قرارداد عمومی HTTP

Headerها:

```http
Authorization: Bearer <access-token>
Content-Type: application/json
```

عملیات Write یک Result برمی‌گردانند:

```ts
export interface ApiResult<T> {
  data: T | null;
  isSuccess: boolean;
  message: string;
}

export interface IdResponse {
  id: number;
}
```

> نام فیلدهای Result را با خروجی Interceptor عمومی پروژه هماهنگ کنید. در خطای Business معمولاً HTTP 400 همراه `isSuccess: false` و پیام فارسی دریافت می‌شود. 401 یعنی Token نامعتبر/منقضی است.

List Queryها مستقیماً ساختار Paging زیر را برمی‌گردانند:

```ts
export interface PaginatedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasPrevious: boolean;
  hasNext: boolean;
}
```

- `page` حداقل 1 است.
- `pageSize` در Backend بین 1 و 100 Clamp می‌شود.
- تمام تاریخ‌ها را ISO-8601 ارسال کنید؛ نمونه: `2026-09-23T00:00:00Z`.
- برای پارامترهای خالی مقدار `null`، رشته خالی یا `undefined` را در URL قرار ندهید.

---

## 4. صفحه ساخت پرونده و فرم داینامیک چک/سفته

### الزام UX اصلی

فرم تعهدات باید **کاملاً داینامیک** باشد. کاربر نباید به یک چک یا یک سفته محدود شود.

در صفحه دو دکمه مستقل قرار دهید:

```text
[ + چک بعدی ]       [ + سفته بعدی ]
```

- کلیک روی **چک بعدی** یک Card/Row جدید چک با فیلدهای مستقل اضافه کند.
- کلیک روی **سفته بعدی** یک Card/Row جدید سفته با فیلدهای مستقل اضافه کند.
- این دو لیست مستقل‌اند؛ کاربر می‌تواند فقط چک، فقط سفته یا ترکیبی از هر دو بسازد.
- هر Card دکمه «حذف از فرم» داشته باشد. این حذف فقط قبل از Submit و از FormArray است و API حذف مالی را صدا نمی‌زند.
- عنوان Cardها شماره‌دار باشد: `چک شماره ۱`، `چک شماره ۲`، `سفته شماره ۱` و ... .
- برای جلوگیری از Submit دوباره، هنگام درخواست کل فرم و دکمه ثبت Disable شود.

ساختار پیشنهادی:

```text
اطلاعات پرونده
├── بیمار
├── خدمت
├── مبلغ کل
└── نوع توافق

تعهدات
├── [چک شماره ۱]
│   ├── مبلغ
│   ├── شماره صیاد
│   ├── نام صاحب چک
│   ├── تاریخ سررسید
│   └── حذف از فرم
├── [چک شماره ۲] ...
├── [سفته شماره ۱]
│   ├── شماره سریال
│   ├── مبلغ
│   ├── تاریخ سررسید
│   └── حذف از فرم
└── [ + چک بعدی ] [ + سفته بعدی ]
```

### رفتار بر اساس AgreementType

- اگر `PrePayment=1` انتخاب شد، مجموع تعداد آیتم‌های دو FormArray باید حداقل 1 باشد؛ در غیر این صورت Submit غیرفعال و پیام «ثبت حداقل یک چک یا سفته الزامی است» نمایش داده شود.
- اگر `Deposit=2` انتخاب شد، هر دو لیست می‌توانند خالی باشند.
- تغییر از Deposit به PrePayment باید فوراً Validator ترکیبی دو آرایه را فعال کند.
- Frontend نباید جمع مبلغ چک‌ها و سفته‌ها را به عنوان پرداخت نمایش دهد. بهتر است آن را با عنوان «مجموع تعهدات ثبت‌شده» نمایش دهد.

### نمونه Angular Reactive Forms

```ts
financialCaseForm = this.fb.group({
  patientId: this.fb.control<number | null>(null, Validators.required),
  serviceId: this.fb.control<number | null>(null, Validators.required),
  totalAmount: this.fb.control<number | null>(null, [Validators.required, Validators.min(1)]),
  agreementType: this.fb.control<1 | 2>(2, Validators.required),
  cheques: this.fb.array<FormGroup>([]),
  promissoryNotes: this.fb.array<FormGroup>([]),
});

get cheques(): FormArray {
  return this.financialCaseForm.controls.cheques;
}

get promissoryNotes(): FormArray {
  return this.financialCaseForm.controls.promissoryNotes;
}

addCheque(): void {
  this.cheques.push(this.fb.group({
    amount: [null, [Validators.required, Validators.min(1)]],
    sayadNumber: ['', [Validators.required]],
    ownerName: ['', [Validators.required]],
    dueDate: [null, [Validators.required]],
  }));
}

addPromissoryNote(): void {
  this.promissoryNotes.push(this.fb.group({
    serialNumber: ['', [Validators.required]],
    amount: [null, [Validators.required, Validators.min(1)]],
    dueDate: [null, [Validators.required]],
  }));
}

removeCheque(index: number): void {
  this.cheques.removeAt(index);
}

removePromissoryNote(index: number): void {
  this.promissoryNotes.removeAt(index);
}
```

Validator ترکیبی:

```ts
function prePaymentCommitmentValidator(control: AbstractControl): ValidationErrors | null {
  const agreementType = control.get('agreementType')?.value;
  const chequeCount = (control.get('cheques') as FormArray)?.length ?? 0;
  const noteCount = (control.get('promissoryNotes') as FormArray)?.length ?? 0;

  return agreementType === 1 && chequeCount + noteCount === 0
    ? { prePaymentRequiresCommitment: true }
    : null;
}
```

### ایجاد پرونده

```http
POST /api/secretary/patient-financial-cases
```

```json
{
  "patientId": 125,
  "serviceId": 1,
  "totalAmount": 150000000,
  "agreementType": 1,
  "cheques": [
    {
      "amount": 30000000,
      "sayadNumber": "1234567890123456",
      "ownerName": "علی رضایی",
      "dueDate": "2026-09-23T00:00:00Z"
    },
    {
      "amount": 20000000,
      "sayadNumber": "9876543210987654",
      "ownerName": "رضا رضایی",
      "dueDate": "2026-10-23T00:00:00Z"
    }
  ],
  "promissoryNotes": [
    {
      "serialNumber": "PN-10020",
      "amount": 25000000,
      "dueDate": "2026-11-23T00:00:00Z"
    }
  ]
}
```

پاسخ موفق:

```json
{
  "data": { "id": 1001 },
  "isSuccess": true,
  "message": ""
}
```

بعد از موفقیت، FormArrayها را Reset کنید و کاربر را به جزئیات `/patient-financial-cases/1001` هدایت کنید.

---

## 5. CRUD پرونده مالی

### لیست پرونده‌ها

```http
GET /api/secretary/patient-financial-cases
```

پارامترها:

| نام | نوع | توضیح |
|---|---|---|
| `search` | string | نام یا تلفن بیمار |
| `patientId` | number | شناسه بیمار |
| `serviceId` | number | شناسه Enum خدمت |
| `agreementType` | 1 یا 2 | نوع توافق |
| `status` | 1 تا 3 | وضعیت پرونده |
| `fromDate` | ISO date | تاریخ ایجاد از |
| `toDate` | ISO date | تاریخ ایجاد تا |
| `page` | number | صفحه |
| `pageSize` | number | تعداد |

نمونه:

```http
GET /api/secretary/patient-financial-cases?search=0912&status=1&page=1&pageSize=20
```

هر Item:

```ts
interface PatientFinancialCase {
  id: number;
  patientId: number;
  userId: string; // شناسه کاربر بیمار (GUID)
  patientName: string;
  patientPhoneNumber: string | null;
  serviceId: number;
  serviceName: string;
  totalAmount: number;
  totalPaidAmount: number;
  remainingAmount: number;
  totalDebtAmount: number;
  agreementType: 1 | 2;
  status: 1 | 2 | 3;
  createdAt: string;
}
```

### جزئیات پرونده

```http
GET /api/secretary/patient-financial-cases/{caseId}
```

```ts
interface PatientFinancialCaseDetails {
  case: PatientFinancialCase;
  chequeCount: number;
  chequeAmount: number;
  promissoryNoteCount: number;
  promissoryNoteAmount: number;
}
```

### ویرایش پرونده

فقط پرونده `Active` قابل ویرایش است.

```http
PUT /api/secretary/patient-financial-cases/{caseId}
```

```json
{
  "totalAmount": 170000000,
  "agreementType": 2
}
```

مبلغ کل جدید نمی‌تواند از مجموع پرداخت قطعی کمتر باشد.

### لغو پرونده

```http
DELETE /api/secretary/patient-financial-cases/{caseId}
```

این عملیات رکورد را حذف نمی‌کند؛ وضعیت را به `Cancelled` تغییر می‌دهد. در UI قبل از ارسال Confirmation نشان دهید. دکمه فقط برای پرونده Active فعال باشد.

---

## 6. افزودن تعهد بعد از ساخت پرونده

در صفحه جزئیات پرونده Active نیز همان UX داینامیک پیشنهاد می‌شود. Modal/Drawer «افزودن تعهد» دو دکمه مستقل **چک بعدی** و **سفته بعدی** دارد. چون API افزودن Child تکی است، برای هر Card یک درخواست ارسال می‌شود.

### افزودن چک

```http
POST /api/secretary/patient-financial-cases/{caseId}/cheques
```

```json
{
  "amount": 30000000,
  "sayadNumber": "1234567890123456",
  "ownerName": "علی رضایی",
  "dueDate": "2026-09-23T00:00:00Z"
}
```

### افزودن سفته

```http
POST /api/secretary/patient-financial-cases/{caseId}/promissory-notes
```

```json
{
  "serialNumber": "PN-10020",
  "amount": 25000000,
  "dueDate": "2026-10-23T00:00:00Z"
}
```

اگر چند Child را بعد از ساخت اضافه می‌کنید:

1. Submit کلی را Disable کنید.
2. درخواست‌ها را کنترل‌شده ارسال کنید (`concatMap` پیشنهاد می‌شود).
3. وضعیت موفق/ناموفق هر Row را مستقل نشان دهید.
4. فقط Rowهای موفق را از فرم حذف کنید تا در Retry دوباره ثبت نشوند.
5. بعد از پایان، لیست چک/سفته و Summary پرونده را Refresh کنید.

---

## 7. لیست و عملیات چک‌ها

### لیست سراسری چک‌ها

```http
GET /api/secretary/patient-cheques
```

Filters: `patientFinancialCaseId`, `patientId`, `search`, `status`, `fromDueDate`, `toDueDate`, `page`, `pageSize`.

```ts
interface PatientCheque {
  id: number;
  patientFinancialCaseId: number;
  patientId: number;
  patientName: string;
  amount: number;
  sayadNumber: string;
  ownerName: string;
  dueDate: string;
  status: 1 | 2 | 3 | 4;
}
```

### تعیین وضعیت چک

```http
PUT /api/secretary/patient-cheques/{chequeId}/status
```

```json
{ "status": 2 }
```

در UI فقط برای چک `Pending` این سه Action نمایش داده شود:

- `Paid=2`: «تأیید وصول»؛ تراکنش پرداخت ایجاد می‌شود.
- `Unpaid=3`: «ثبت چک برگشتی»؛ Debt ایجاد می‌شود و تراکنش پرداخت ایجاد نمی‌شود.
- `Cancelled=4`: «لغو چک»؛ پرداخت محسوب نمی‌شود.

Action حساس Paid/Unpaid حتماً Confirmation داشته باشد. بعد از پاسخ موفق، Row و Summary را از Backend Reload کنید. Transition مجدد یا تغییر `Paid → Unpaid` مجاز نیست.

---

## 8. لیست و عملیات سفته‌ها

```http
GET /api/secretary/patient-promissory-notes
```

Filters: `patientFinancialCaseId`, `patientId`, `search`, `status`, `fromDueDate`, `toDueDate`, `page`, `pageSize`.

```ts
interface PatientPromissoryNote {
  id: number;
  patientFinancialCaseId: number;
  patientId: number;
  patientName: string;
  serialNumber: string;
  amount: number;
  dueDate: string;
  status: 1 | 2 | 3 | 4;
}
```

تغییر وضعیت:

```http
PUT /api/secretary/patient-promissory-notes/{promissoryNoteId}/status
Content-Type: application/json

{ "status": 2 }
```

رفتار و محدودیت‌ها دقیقاً مشابه چک است.

---

## 9. بدهی‌ها و تسویه

### لیست بدهی‌ها

```http
GET /api/secretary/patient-debts
```

Filters:

- `patientId`
- `patientFinancialCaseId`
- `sourceType` (`1=Cheque`, `2=PromissoryNote`)
- `status`
- `year` و `month` شمسی
- `fromDueDate` و `toDueDate`
- `search`
- `page` و `pageSize`

برای ماه شمسی باید `year` و `month` با هم ارسال شوند:

```http
GET /api/secretary/patient-debts?year=1405&month=7&status=1&page=1&pageSize=20
```

```ts
interface PatientDebt {
  id: number;
  patientId: number;
  patientName: string;
  patientPhoneNumber: string | null;
  patientFinancialCaseId: number;
  serviceName: string;
  amount: number;
  sourceType: 1 | 2;
  sourceId: number;
  dueDate: string;
  status: 1 | 2 | 3;
}
```

### تسویه بدهی

```http
POST /api/secretary/patient-debts/{debtId}/pay
```

Body ندارد. فقط برای Debt با وضعیت `Unpaid` دکمه «تسویه بدهی» فعال باشد. این عملیات Transaction ایجاد می‌کند و وضعیت Source اصلی را نیز Paid می‌کند. برای جلوگیری از Double Submit تا پایان درخواست دکمه Disable شود.

---

## 10. تراکنش‌های پرداخت

```http
GET /api/secretary/patient-financial-transactions
```

Filters: `patientId`, `patientFinancialCaseId`, `sourceType`, `fromDate`, `toDate`, `page`, `pageSize`.

```ts
interface PatientFinancialTransaction {
  id: number;
  patientFinancialCaseId: number;
  patientId: number;
  amount: number;
  type: 1;
  sourceType: 1 | 2;
  sourceId: number;
  createdAt: string;
}
```

تراکنش‌ها History قطعی هستند و UI نباید Delete/Edit برای آن‌ها نمایش دهد.

---

## 11. Summary پرونده و بیمار

### Summary یک پرونده

```http
GET /api/secretary/patient-financial-cases/{caseId}/summary
```

```ts
interface PatientFinancialCaseSummary {
  totalAmount: number;
  totalPaidAmount: number;
  remainingAmount: number;
  totalChequeAmount: number;
  paidChequeAmount: number;
  pendingChequeAmount: number;
  unpaidChequeAmount: number;
  totalPromissoryNoteAmount: number;
  paidPromissoryNoteAmount: number;
  pendingPromissoryNoteAmount: number;
  unpaidPromissoryNoteAmount: number;
  totalDebtAmount: number;
}
```

کارت‌های پیشنهادی صفحه:

1. مبلغ کل درمان
2. پرداخت قطعی
3. مانده
4. بدهی باز
5. چک‌های Pending/Paid/Unpaid
6. سفته‌های Pending/Paid/Unpaid

### Summary بیمار

```http
GET /api/secretary/patients/{patientId}/financial-summary
```

```ts
interface PatientFinancialSummary {
  patientId: number;
  totalTreatmentAmount: number;
  totalPaidAmount: number;
  remainingAmount: number;
  totalDebtAmount: number;
  activeFinancialCasesCount: number;
  unpaidChequesCount: number;
  unpaidPromissoryNotesCount: number;
}
```

---

## 12. تعهدات نزدیک سررسید

```http
GET /api/secretary/patient-financial-commitments/due
```

Filters: `fromDate`, `toDate`, `type`, `patientId`, `page`, `pageSize`.

- اگر تاریخ ارسال نشود، Backend امروز تا هفت روز آینده را برمی‌گرداند.
- فقط تعهدات Pending نمایش داده می‌شوند.
- `type=1` فقط چک، `type=2` فقط سفته و بدون Type هر دو را برمی‌گرداند.

```ts
interface PatientFinancialCommitment {
  id: number;
  type: 1 | 2;
  patientFinancialCaseId: number;
  patientId: number;
  patientName: string;
  amount: number;
  dueDate: string;
  status: number;
}
```

Tabهای پیشنهادی: «امروز»، «فردا»، «این هفته»، «بازه دلخواه» و Filter نوع تعهد.

---

## 13. ساختار پیشنهادی Frontend

```text
patient-finance/
├── models/
│   ├── patient-finance.enums.ts
│   ├── patient-financial-case.model.ts
│   ├── patient-cheque.model.ts
│   ├── patient-promissory-note.model.ts
│   ├── patient-debt.model.ts
│   └── patient-financial-transaction.model.ts
├── services/
│   └── patient-finance-api.service.ts
├── pages/
│   ├── financial-case-list/
│   ├── financial-case-create/
│   ├── financial-case-details/
│   ├── cheque-list/
│   ├── promissory-note-list/
│   ├── debt-list/
│   ├── transaction-list/
│   └── due-commitments/
└── components/
    ├── dynamic-cheques-form/
    ├── dynamic-promissory-notes-form/
    ├── financial-summary-cards/
    └── commitment-status-actions/
```

Service پیشنهادی:

```ts
@Injectable({ providedIn: 'root' })
export class PatientFinanceApiService {
  private readonly baseUrl = `${environment.apiUrl}/api/secretary`;

  constructor(private readonly http: HttpClient) {}

  createCase(body: CreatePatientFinancialCaseRequest) {
    return this.http.post<ApiResult<IdResponse>>(`${this.baseUrl}/patient-financial-cases`, body);
  }

  addCheque(caseId: number, body: CreatePatientChequeRequest) {
    return this.http.post<ApiResult<IdResponse>>(`${this.baseUrl}/patient-financial-cases/${caseId}/cheques`, body);
  }

  addPromissoryNote(caseId: number, body: CreatePatientPromissoryNoteRequest) {
    return this.http.post<ApiResult<IdResponse>>(`${this.baseUrl}/patient-financial-cases/${caseId}/promissory-notes`, body);
  }

  updateChequeStatus(id: number, status: 2 | 3 | 4) {
    return this.http.put<ApiResult<IdResponse>>(`${this.baseUrl}/patient-cheques/${id}/status`, { status });
  }

  payDebt(id: number) {
    return this.http.post<ApiResult<IdResponse>>(`${this.baseUrl}/patient-debts/${id}/pay`, null);
  }
}
```

---

## 14. مدیریت Error و Refresh داده‌ها

پیام Business برگشتی Backend را مستقیم در Toast/Dialog نشان دهید. خطاهای متداول:

- بیمار یا خدمت معتبر نیست.
- برای PrePayment حداقل یک چک یا سفته الزامی است.
- پرونده Active یافت نشد.
- وضعیت چک/سفته قبلاً تعیین شده است.
- برای Source قبلاً بدهی ثبت شده است.
- تعهد قبلاً پرداخت شده است.
- پرداخت از مبلغ کل درمان بیشتر می‌شود.
- مبلغ کل جدید کمتر از پرداخت قطعی است.

بعد از هر Mutation موفق، حداقل داده‌های زیر Invalid/Reload شوند:

| Mutation | داده‌های نیازمند Refresh |
|---|---|
| Create/Update/Cancel Case | Case list، Details، Patient Summary |
| Add Cheque/Note | Child list، Case Details، Case Summary |
| Paid/Unpaid/Cancelled | Child list، Case Summary، Patient Summary، Debt/Transaction list |
| Pay Debt | Debt list، Source list، Transactions، Case Summary، Patient Summary |

---

## 15. Acceptance Checklist فرانت

- [ ] JWT در تمام Requestها ارسال می‌شود و `secretaryUserId` هیچ‌جا از Client ارسال نمی‌شود.
- [ ] فرم ساخت دارای دو FormArray مستقل چک و سفته است.
- [ ] دکمه‌های «چک بعدی» و «سفته بعدی» هم‌زمان در دسترس‌اند.
- [ ] تعداد نامحدود Row قابل اضافه‌شدن است و هر Row قبل از Submit قابل حذف است.
- [ ] PrePayment بدون هیچ تعهدی Submit نمی‌شود؛ Deposit بدون تعهد مجاز است.
- [ ] شماره صیاد، صاحب چک، سریال سفته، مبلغ مثبت و DueDate در Client Validate می‌شوند.
- [ ] تاریخ‌ها ISO ارسال و تاریخ شمسی فقط در Presentation استفاده می‌شود.
- [ ] ثبت چک/سفته به عنوان پرداخت نمایش داده نمی‌شود.
- [ ] Action وضعیت فقط برای Pending فعال است و Confirmation دارد.
- [ ] دکمه‌های Mutation هنگام Request Disable هستند تا Double Click رخ ندهد.
- [ ] مبلغ Paid، Remaining و Debt از پاسخ Backend خوانده می‌شود.
- [ ] تمام Listها Paging و Filterهای تعریف‌شده را دارند.
- [ ] Year و Month شمسی بدهی همیشه با هم ارسال می‌شوند.
- [ ] بعد از Mutation، Summary و Listهای مرتبط Reload می‌شوند.
- [ ] برای تراکنش قطعی دکمه Edit/Delete نمایش داده نمی‌شود.
- [ ] Cancel Case به‌عنوان لغو وضعیت نمایش داده می‌شود، نه حذف سابقه مالی.
