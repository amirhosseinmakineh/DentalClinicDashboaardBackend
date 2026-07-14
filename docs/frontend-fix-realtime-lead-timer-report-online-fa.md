# رفع باگ: تایمر لید، ثبت گزارش، و آنلاین نشدن مشاور

## مشکلات گزارش‌شده

1. بعد از برداشتن لید، پیام «مهلت تمام شد / گزارش بزنید» نمایش داده می‌شود.
2. تایمر ۲۰ دقیقه‌ای فعال نمی‌شود.
3. دکمه «ثبت گزارش» غیرفعال می‌ماند یا قابل کلیک نیست.
4. بعد از ثبت گزارش، کارت لید روی «در حال ثبت گزارش» گیر می‌کند.
5. بعد از ثبت همه گزارش‌ها، مشاور به‌صورت خودکار آنلاین نمی‌شود (در UI).

## علت

### تایمر و مودال گزارش

- متد `openReportForDueRealtimeLeads` بلافاصله بعد از برداشتن لید اجرا می‌شد.
- برای لیدهایی که `requiresThreeMinuteCall` غیرفعال بود یا تایمر هنوز hydrate نشده بود، `leadRemainingMs` مقدار `0` برمی‌گرداند.
- نتیجه: پیام «مهلت تماس تمام شد» و باز شدن اجباری مودال گزارش، بدون شروع تایمر.

### ثبت گزارش و وضعیت آنلاین

- بعد از موفقیت API در `submitLeadReport`، مجموعه `reportingLeadIds` پاک نمی‌شد.
- پاسخ API (`isConsultantOnline`, `leadAssignmentState`, `callResult`) بلافاصله روی state محلی اعمال نمی‌شد.
- فیلدهای PascalCase (`IsOnline`, `IsConsultantOnline`) در parse وضعیت خوانده نمی‌شدند.

## راه‌حل (یک patch واحد)

```bash
cd /path/to/drsaeedMoghadamFront
git checkout -b cursor/fix-realtime-lead-timer-report-online-d8fa
git apply --check ../DentalClinicDashboaardBackend/docs/patches/fix-realtime-lead-timer-report-online-frontend.patch
git apply ../DentalClinicDashboaardBackend/docs/patches/fix-realtime-lead-timer-report-online-frontend.patch
git add -A
git commit -m "Fix realtime lead timer, report submit, and online status sync"
git push -u origin cursor/fix-realtime-lead-timer-report-online-d8fa
```

## فایل‌های تغییر یافته

- `src/app/pages/consultant-dashboard/consultant-dashboard.component.ts`
- `src/app/core/consultant/consultant-dashboard.service.ts`

## رفتار صحیح بعد از اعمال

1. برداشتن لید → تایمر ۲۰ دقیقه‌ای شروع می‌شود (بدون باز شدن خودکار مودال).
2. دکمه «ثبت گزارش» فعال است و فقط با کلیک کاربر مودال باز می‌شود.
3. بعد از ثبت گزارش → وضعیت کارت به‌روز می‌شود و «در حال ثبت گزارش» نمی‌ماند.
4. اگر بکند مشاور را آنلاین کند (ساعات کاری)، UI بلافاصله آنلاین را نشان می‌دهد.
