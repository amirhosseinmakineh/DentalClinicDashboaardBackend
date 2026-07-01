# مستند فرانت‌اند: گزارش‌گیری اکسل داشبورد ادمین

این مستند برای پیاده‌سازی بخش گزارش‌گیری در داشبورد ادمین آماده شده است.

## هدف

در هر بخش ادمین، ادمین باید بتواند با یک دکمه، فایل اکسل (CSV) فارسی دانلود کند:

| بخش داشبورد ادمین | دکمه پیشنهادی | Endpoint |
|---|---|---|
| کاربران | «دانلود گزارش کاربران» | `GET /api/admin/reports/users/export` |
| لیدها | «دانلود گزارش لیدها» | `GET /api/admin/reports/leads/export` |
| مشاوران | «دانلود گزارش مشاوران» | `GET /api/admin/reports/consultants/export` |
| گزارش تماس لیدها (اختیاری) | «دانلود گزارش تماس» + انتخاب بازه تاریخ | `GET /api/admin/reports/lead-call-reports/export` |

> گزارش مشاوران مهم‌ترین گزارش است و باید در صفحه مشاوران به‌صورت برجسته (دکمه اصلی) قرار بگیرد.

---

## جریان کلی UI

```
ادمین وارد بخش مربوطه می‌شود
        ↓
روی دکمه «دانلود گزارش» کلیک می‌کند
        ↓
فرانت loading نمایش می‌دهد
        ↓
GET به endpoint مربوطه (با responseType: blob)
        ↓
فایل CSV دانلود می‌شود
        ↓
ادمین فایل را در Excel باز می‌کند
```

- گزارش‌ها **فقط دستی** و با کلیک ادمین تولید می‌شوند.
- هیچ polling یا background job وجود ندارد.
- نیازی به نمایش preview جدول در فرانت نیست؛ فقط دانلود فایل کافی است.

---

## مشخصات فنی API

### Base URL

```
{API_BASE_URL}/api/admin/reports
```

### Headers

```http
Authorization: Bearer {token}
Accept: text/csv
```

> در حال حاضر endpointها `[Authorize]` ندارند، اما توصیه می‌شود فقط در داشبورد ادمین (`role = Admin`) این دکمه‌ها نمایش داده شوند و توکن ارسال شود.

### Response موفق

| فیلد | مقدار |
|---|---|
| Status | `200 OK` |
| Content-Type | `text/csv; charset=utf-8` |
| Body | فایل باینری CSV با BOM (UTF-8) |
| Content-Disposition | نام فایل در header (مثلاً `users-report-20260701.csv`) |

### Response خطا

| Status | معنی |
|---:|---|
| `500` | خطای سرور — پیام عمومی به کاربر نمایش دهید |
| `0` / network error | مشکل اتصال |

---

## ۱. گزارش کاربران

### Endpoint

```
GET /api/admin/reports/users/export
```

### Query params

ندارد.

### نام فایل دانلودی

```
users-report-YYYYMMDD.csv
```

### ستون‌های فایل (همه فارسی)

| ستون | توضیح | نمونه |
|---|---|---|
| شناسه | GUID کاربر | `a1b2c3d4-...` |
| نام | نام | `علی` |
| نام خانوادگی | نام خانوادگی | `احمدی` |
| موبایل | شماره موبایل | `09120000000` |
| نقش | نقش کاربر | `ادمین` / `مشاور` / `بیمار` / `کاربر` |
| جنسیت | جنسیت | `مرد` / `زن` |
| تاریخ تولد | شمسی | `1375/03/15` |
| وضعیت فعال | فعال/غیرفعال | `بله` / `خیر` |
| پروفایل تکمیل شده | | `بله` / `خیر` |
| تاریخ ثبت‌نام | شمسی با ساعت | `1405/04/10 14:30:00` |

### UI پیشنهادی

- محل قرارگیری: بالای جدول لیست کاربران (`/admin/users`)
- دکمه: `دانلود گزارش اکسل کاربران`
- آیکون: download / excel
- بدون فیلتر تاریخ

---

## ۲. گزارش لیدها

### Endpoint

```
GET /api/admin/reports/leads/export
```

### Query params

ندارد.

### نام فایل دانلودی

```
leads-report-YYYYMMDD.csv
```

### ستون‌های فایل

| ستون | توضیح | مقادیر مهم |
|---|---|---|
| شناسه لید | | `123` |
| نام لید | | `رضا محمدی` |
| موبایل لید | | `09121111111` |
| وضعیت لید | | `جدید` / `تخصیص‌یافته` / `تماس گرفته شده` / `تبدیل شده` / `منقضی شده` / `رد شده` |
| نوع تخصیص | | `آنی` / `صف آفلاین` |
| **وضعیت اساین** | **مهم** | `اساین شده` / `اساین نشده` |
| نام مشاور | اگر اساین شده | `سارا رضایی` |
| موبایل مشاور | اگر اساین شده | `09123333333` |
| تاریخ تخصیص | شمسی | `1405/04/10 09:15:00` |
| **وضعیت تماس** | **مهم** | `تماس گرفته` / `تماس نگرفته` |
| نتیجه تماس | اگر تماس گرفته | `تماس برقرار شد` / `پاسخ نداد` / `تبدیل به رزرو` / ... |
| تاریخ ثبت گزارش | شمسی | |
| تاریخ تماس | شمسی | |
| تاریخ ایجاد لید | شمسی | |

### UI پیشنهادی

- محل قرارگیری: بالای جدول لیست لیدها (`/admin/leads`)
- دکمه: `دانلود گزارش اکسل لیدها`
- توضیح کوتاه زیر دکمه: «شامل وضعیت اساین و اطلاعات مشاور»

---

## ۳. گزارش مشاوران (مهم‌ترین)

### Endpoint

```
GET /api/admin/reports/consultants/export
```

### Query params

ندارد.

### نام فایل دانلودی

```
consultants-report-YYYYMMDD.csv
```

### ساختار فایل

فایل **دو بخش** دارد (در یک sheet اکسل):

#### بخش ۱ — خلاصه مشاوران

| ستون | توضیح |
|---|---|
| شناسه مشاور | |
| نام | |
| نام خانوادگی | |
| موبایل | |
| کد ملی | |
| امتیاز فعلی | عدد |
| وضعیت آنلاین | `بله` / `خیر` |
| وضعیت حضور | `بله` / `خیر` |
| تعداد کل لیدها | |
| تعداد تماس گرفته | |
| تعداد تماس نگرفته | |
| تعداد تبدیل شده | |
| تعداد رد شده | |
| تعداد منقضی شده | |

#### بخش ۲ — جزئیات تماس لیدها

یک ردیف خالی جداکننده، سپس:

| ستون | توضیح |
|---|---|
| شناسه مشاور | |
| نام مشاور | |
| موبایل مشاور | |
| شناسه لید | |
| نام لید | |
| موبایل لید | |
| وضعیت لید | |
| نوع تخصیص | |
| تاریخ تخصیص | |
| وضعیت تماس | `تماس گرفته` / `تماس نگرفته` |
| نتیجه تماس | |
| تاریخ تماس | |
| تاریخ ثبت گزارش | |
| متن گزارش | |
| شهر بیمار | |
| منطقه بیمار | |
| نام بیزینس | |
| احتمال حضور (درصد) | |

### UI پیشنهادی

- محل قرارگیری: بالای صفحه مشاوران (`/admin/consultants`)
- دکمه اصلی (برجسته): `دانلود گزارش کامل مشاوران`
- tooltip: «شامل خلاصه عملکرد و جزئیات تماس هر مشاور با لیدها»

---

## ۴. گزارش تماس لیدها (بازه زمانی — اختیاری)

### Endpoint

```
GET /api/admin/reports/lead-call-reports/export?from=2026-06-01&to=2026-06-28
```

### Query params

| پارامتر | نوع | اجباری | توضیح |
|---|---|:---:|---|
| `from` | `DateTime` (ISO) | خیر | تاریخ شروع (میلادی) — مثال: `2026-06-01` |
| `to` | `DateTime` (ISO) | خیر | تاریخ پایان (میلادی) — مثال: `2026-06-28` |

- اگر تاریخ ارسال نشود، گزارش **روز جاری** برگردانده می‌شود.
- فقط لیدهایی که **گزارش تماس ثبت‌شده** دارند در این بازه می‌آیند.

### UI پیشنهادی

- دو date picker (از / تا) + دکمه «دانلود گزارش تماس»
- می‌تواند در صفحه لیدها یا یک بخش جداگانه «گزارش تماس» باشد

---

## پیاده‌سازی Angular

### ۱. سرویس گزارش ادمین

```typescript
// admin-reports.service.ts
import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '@env/environment';

export type AdminReportType = 'users' | 'leads' | 'consultants' | 'lead-call-reports';

@Injectable({ providedIn: 'root' })
export class AdminReportsService {
  private readonly baseUrl = `${environment.apiUrl}/api/admin/reports`;

  constructor(private http: HttpClient) {}

  downloadUsersReport(): Observable<Blob> {
    return this.http.get(`${this.baseUrl}/users/export`, {
      responseType: 'blob',
    });
  }

  downloadLeadsReport(): Observable<Blob> {
    return this.http.get(`${this.baseUrl}/leads/export`, {
      responseType: 'blob',
    });
  }

  downloadConsultantsReport(): Observable<Blob> {
    return this.http.get(`${this.baseUrl}/consultants/export`, {
      responseType: 'blob',
    });
  }

  downloadLeadCallReports(from?: string, to?: string): Observable<Blob> {
    let params = new HttpParams();
    if (from) params = params.set('from', from);
    if (to) params = params.set('to', to);

    return this.http.get(`${this.baseUrl}/lead-call-reports/export`, {
      params,
      responseType: 'blob',
    });
  }
}
```

### ۲. Helper دانلود فایل

```typescript
// file-download.util.ts
export function downloadBlob(blob: Blob, filename: string): void {
  const url = window.URL.createObjectURL(blob);
  const anchor = document.createElement('a');
  anchor.href = url;
  anchor.download = filename;
  anchor.click();
  window.URL.revokeObjectURL(url);
}

export function extractFilename(contentDisposition: string | null, fallback: string): string {
  if (!contentDisposition) return fallback;

  const match = /filename[^;=\n]*=((['"]).*?\2|[^;\n]*)/.exec(contentDisposition);
  if (match?.[1]) {
    return match[1].replace(/['"]/g, '');
  }

  return fallback;
}
```

### ۳. نمونه کامپوننت (بخش کاربران)

```typescript
// admin-users.component.ts
import { Component } from '@angular/core';
import { finalize } from 'rxjs/operators';
import { AdminReportsService } from '@app/services/admin-reports.service';
import { downloadBlob } from '@app/utils/file-download.util';

@Component({
  selector: 'app-admin-users',
  templateUrl: './admin-users.component.html',
})
export class AdminUsersComponent {
  isExporting = false;

  constructor(private adminReportsService: AdminReportsService) {}

  exportUsers(): void {
    if (this.isExporting) return;

    this.isExporting = true;

    this.adminReportsService
      .downloadUsersReport()
      .pipe(finalize(() => (this.isExporting = false)))
      .subscribe({
        next: (blob) => {
          const today = new Date().toISOString().slice(0, 10).replace(/-/g, '');
          downloadBlob(blob, `users-report-${today}.csv`);
        },
        error: () => {
          // toast یا snackbar
          alert('خطا در دریافت گزارش. لطفاً دوباره تلاش کنید.');
        },
      });
  }
}
```

```html
<!-- admin-users.component.html -->
<button
  type="button"
  class="btn btn-outline-success"
  [disabled]="isExporting"
  (click)="exportUsers()">
  <span *ngIf="!isExporting">دانلود گزارش اکسل کاربران</span>
  <span *ngIf="isExporting">در حال آماده‌سازی...</span>
</button>
```

### ۴. نمونه بخش مشاوران

```typescript
exportConsultants(): void {
  if (this.isExporting) return;
  this.isExporting = true;

  this.adminReportsService
    .downloadConsultantsReport()
    .pipe(finalize(() => (this.isExporting = false)))
    .subscribe({
      next: (blob) => {
        const today = new Date().toISOString().slice(0, 10).replace(/-/g, '');
        downloadBlob(blob, `consultants-report-${today}.csv`);
      },
      error: () => alert('خطا در دریافت گزارش مشاوران.'),
    });
}
```

### ۵. نمونه گزارش تماس با بازه تاریخ

```typescript
fromDate = ''; // مثال: '2026-06-01'
toDate = '';   // مثال: '2026-06-28'

exportLeadCallReports(): void {
  this.isExporting = true;

  this.adminReportsService
    .downloadLeadCallReports(this.fromDate || undefined, this.toDate || undefined)
    .pipe(finalize(() => (this.isExporting = false)))
    .subscribe({
      next: (blob) => downloadBlob(blob, 'lead-call-reports.csv'),
      error: () => alert('خطا در دریافت گزارش تماس لیدها.'),
    });
}
```

---

## نکات مهم پیاده‌سازی

### ۱. responseType باید blob باشد

```typescript
// درست
this.http.get(url, { responseType: 'blob' })

// اشتباه — JSON parse می‌شود و فایل خراب می‌شود
this.http.get(url)
```

### ۲. Interceptor

اگر interceptor شما به‌صورت پیش‌فرض `responseType` را تغییر می‌دهد یا error handling برای blob دارد، مطمئن شوید برای این endpointها exception در نظر گرفته شده باشد.

### ۳. فایل CSV در Excel

- فایل با BOM یونیکد UTF-8 است و فارسی درست نمایش داده می‌شود.
- پسوند `.csv` کافی است؛ نیازی به تبدیل به `.xlsx` در فرانت نیست.
- اگر کاربر خواست، می‌تواند در Excel با «Save As» به xlsx تبدیل کند.

### ۴. دسترسی

فقط کاربر با نقش `Admin` باید این دکمه‌ها را ببیند:

```typescript
// بعد از login
if (user.role === 'Admin' || user.roles?.includes('Admin')) {
  this.showExportButton = true;
}
```

مسیر داشبورد ادمین از API لاگین: `/admin/dashboard`

### ۵. Loading و جلوگیری از کلیک مکرر

- هنگام دانلود `disabled` کنید
- متن دکمه را به «در حال آماده‌سازی...» تغییر دهید
- از `finalize` برای reset loading استفاده کنید

---

## چک‌لیست پیاده‌سازی فرانت

### صفحه کاربران (`/admin/users`)
- [ ] دکمه «دانلود گزارش اکسل کاربران»
- [ ] فراخوانی `GET /api/admin/reports/users/export`
- [ ] نمایش loading
- [ ] دانلود فایل CSV

### صفحه لیدها (`/admin/leads`)
- [ ] دکمه «دانلود گزارش اکسل لیدها»
- [ ] فراخوانی `GET /api/admin/reports/leads/export`
- [ ] نمایش loading
- [ ] دانلود فایل CSV

### صفحه مشاوران (`/admin/consultants`)
- [ ] دکمه برجسته «دانلود گزارش کامل مشاوران»
- [ ] فراخوانی `GET /api/admin/reports/consultants/export`
- [ ] نمایش loading
- [ ] دانلود فایل CSV

### گزارش تماس (اختیاری)
- [ ] date picker از / تا
- [ ] دکمه «دانلود گزارش تماس»
- [ ] فراخوانی `GET /api/admin/reports/lead-call-reports/export?from=&to=`

### عمومی
- [ ] فقط نقش Admin دسترسی دارد
- [ ] `responseType: 'blob'`
- [ ] مدیریت خطا با toast/message
- [ ] جلوگیری از double-click

---

## تست دستی

1. با کاربر Admin لاگین کنید.
2. به هر بخش بروید و دکمه دانلود را بزنید.
3. فایل CSV دانلود شود.
4. فایل را در Excel باز کنید.
5. بررسی کنید:
   - ستون‌ها فارسی هستند
   - مقادیر وضعیت فارسی هستند (نه `Assigned` یا `Contacted`)
   - تاریخ‌ها شمسی هستند
6. در گزارش لیدها: لیدهای بدون مشاور → `اساین نشده`
7. در گزارش مشاوران: بخش خلاصه و بخش جزئیات هر دو وجود داشته باشند

---

## سوالات متداول

**آیا باید جدول گزارش را در UI نمایش دهیم؟**
خیر. فقط دانلود فایل کافی است.

**آیا فیلتر روی گزارش کاربران/لیدها/مشاوران داریم؟**
خیر. همه رکوردها export می‌شوند. فقط گزارش تماس لیدها بازه تاریخ دارد.

**فایل xlsx است یا csv؟**
CSV با BOM که در Excel باز می‌شود. نام فایل `.csv` است.

**اگر داده‌ای نباشد چه می‌شود؟**
فایل با فقط header (ستون‌ها) برگردانده می‌شود.
