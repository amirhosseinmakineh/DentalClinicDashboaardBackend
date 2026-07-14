# رفع باگ: وضعیت لید بعد از ثبت گزارش و آنلاین نشدن مشاور

## مشکلات

1. بعد از ثبت گزارش، کارت لید همچنان «در حال ثبت گزارش» نشان می‌داد.
2. بعد از ثبت گزارش، مشاور آنلاین نمی‌شد (در UI).

## علت

### فرانت (`consultant-dashboard.component.ts`)

- در `submitLeadReport` پس از موفقیت API، `reportingLeadIds` پاک نمی‌شد.
- `closeReportDialog({ releaseReportLock: false })` باعث می‌شد قفل گزارش آزاد نشود.
- منطق `closeReportDialog` فقط وقتی `reportedLeadIds` خالی بود `reportingLeadIds` را پاک می‌کرد.
- برخلاف `submitEditedLeadReport`، پاسخ API (`leadAssignmentState`, `callResult`, `isConsultantOnline`) بلافاصله روی state محلی اعمال نمی‌شد.
- `leadDisplayStatus` بعد از ثبت، نتیجه تماس انتخاب‌شده را نشان نمی‌داد.

### بکند

- منطق `SubmitLeadCallReportCommandHandler` برای آنلاین کردن مشاور در ساعات کاری درست بود، اما رویداد حضور (`Online`) در لاگ ثبت نمی‌شد.

## راه‌حل

### بکند (این ریپو)

- ثبت لاگ حضور هنگام آنلاین شدن خودکار بعد از ثبت گزارش.
- اصلاح `frontend-reference` برای parse درست `GetDashboardStatus` (پاسخ تخت بدون `data`).

### فرانت (ریپوی `drsaeedMoghadamFront`)

اعمال patch:

```bash
cd /path/to/drsaeedMoghadamFront
git checkout -b cursor/fix-report-submit-state-online-daaf
git apply --check ../DentalClinicDashboaardBackend/docs/patches/fix-report-submit-state-online-frontend.patch
git apply ../DentalClinicDashboaardBackend/docs/patches/fix-report-submit-state-online-frontend.patch
git add -A
git commit -m "Fix lead status and consultant online state after report submit"
git push -u origin cursor/fix-report-submit-state-online-daaf
```

## فایل‌های تغییر یافته

- `src/app/pages/consultant-dashboard/consultant-dashboard.component.ts`
- `src/app/core/consultant/consultant-dashboard.service.ts`
