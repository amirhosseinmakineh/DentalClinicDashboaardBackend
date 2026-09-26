# راهنمای اصلاح فیلتر تاریخ لیدها در داشبورد مشاور (فرانت‌اند)

## مشکل

فرانت‌اند فعلی در `loadLeads()` این پارامترها را می‌فرستد:

```typescript
this.consultantApi.getLeads({
  profileId,
  fromDate: this.formatFilterDate(this.leadFromDate),   // ❌ بک‌اند قدیمی/جدید: from
  toDate: this.formatFilterDate(this.leadToDate),         // ❌ بک‌اند: to
  leadActivityFilter: this.leadActivityFilter,            // ❌ حذف شده از API
  ...
});
```

بک‌اند فقط `from` / `to` / `date` را می‌پذیرد و فیلتر را روی `AssignedAt` اعمال می‌کند.

> **تا زمان deploy بک‌اند جدید:** سازگاری `fromDate`/`toDate` در بک‌اند اضافه شده؛ بعد از deploy باید فیلتر کار کند حتی بدون تغییر فرانت.
> **توصیه:** فیلتر «نوع فعالیت» را از UI حذف کنید و پارامترها را به `from`/`to` تغییر دهید.

## تغییرات لازم در فرانت‌اند

فایل: `src/app/pages/consultant-dashboard/consultant-dashboard.component.ts`

### ۱. اصلاح درخواست API در `loadLeads()`

```diff
 this.consultantApi.getLeads({
   profileId: t,
   leadAssignmentState: this.effectiveLeadStateFilter(),
   leadAssignmentType: this.leadTypeFilter,
-  fromDate: this.formatFilterDate(this.leadFromDate),
-  toDate: this.formatFilterDate(this.leadToDate),
-  leadActivityFilter: this.leadActivityFilter,
+  from: this.formatFilterDate(this.leadFromDate),
+  to: this.formatFilterDate(this.leadToDate),
   pageNumber: this.leadPageNumber,
   pageSize: this.leadPageSize,
 })
```

### ۲. حذف فیلتر نوع فعالیت

- پراپرتی `leadActivityFilter` را حذف کنید.
- در HTML بلوک `<select>` با برچسب «نوع فعالیت» را حذف کنید.

### ۳. (اختیاری) یک‌روزه کردن فیلتر

اگر کاربر فقط «از تاریخ» را انتخاب می‌کند، می‌توانید `to` را همان روز بگذارید:

```typescript
const from = this.formatFilterDate(this.leadFromDate);
const to = this.formatFilterDate(this.leadToDate ?? this.leadFromDate);
```

## SQL

نیازی به تغییر دیتابیس نیست. ستون `AssignedAt` از قبل روی `LeadAssignments` وجود دارد.

برای بررسی دستی:

```sql
SELECT Id, UserName, AssignedAt, LeadAssignmentState
FROM LeadAssignments
WHERE ConsultantProfileId = 59
  AND IsDeleted = 0
  AND AssignedAt >= '2026-07-16'
  AND AssignedAt < '2026-07-17'
ORDER BY AssignedAt DESC;
```

## تست بعد از deploy

```http
GET /api/Consultant/GetLeads?profileId=59&fromDate=2026-07-16&toDate=2026-07-16&pageNumber=1&pageSize=10
```

باید فقط لیدهای assign‌شده در آن روز برگردد (نه همه لیدها).

```http
GET /api/Consultant/GetLeads?profileId=59&fromDate=2026-07-22&toDate=2026-07-22&pageNumber=1&pageSize=10
```

اگر لیدی در آن روز assign نشده، باید `totalCount: 0` باشد.
