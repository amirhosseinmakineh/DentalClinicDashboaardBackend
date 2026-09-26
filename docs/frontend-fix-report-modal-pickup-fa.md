# رفع باگ فرانت: باز شدن خودکار مودال ثبت گزارش بعد از برداشتن لید

## مشکل

بعد از برداشتن لید لحظه‌ای، مودال «ثبت گزارش» بلافاصله باز می‌شد؛ در حالی که باید فقط تایمر ۲۰ دقیقه‌ای فعال شود و مودال فقط با کلیک روی دکمه «ثبت گزارش» باز شود (و در حالت ثبت جدید قابل بستن نباشد).

## علت

در `consultant-dashboard.component.ts` متد `openReportForDueRealtimeLeads` بعد از `loadLeads` و هر ثانیه روی تایمر اجرا می‌شد و اگر `leadRemainingMs` صفر برمی‌گرداند، مودال را باز می‌کرد. برای لیدهایی که `requiresThreeMinuteCall` غیرفعال بود یا تایمر هنوز hydrate نشده بود، `leadRemainingMs` اشتباهاً `0` برمی‌گرداند.

## راه‌حل

1. حذف باز شدن خودکار مودال (`openReportForDueRealtimeLeads`)
2. شروع صریح تایمر ۲۰ دقیقه‌ای هنگام رویداد `consultant-lead-picked-up`
3. نمایش تایمر برای همه لیدهای لحظه‌ای فعال (بدون وابستگی به `requiresThreeMinuteCall`)
4. باز شدن مودال فقط از دکمه «ثبت گزارش» — در حالت `create` همچنان `[closable]="false"`

## اعمال patch

```bash
cd D:\DentalDashboard\DentalDashboardFront\drsaeedMoghadamFront
git checkout -b cursor/fix-report-modal-pickup-3c94
git apply --check ..\DentalClinicDashboaardBackend\docs\patches\fix-report-modal-pickup-frontend.patch
git apply ..\DentalClinicDashboaardBackend\docs\patches\fix-report-modal-pickup-frontend.patch
git add -A
git commit -m "Fix report modal opening on lead pickup instead of timer start"
git push -u origin cursor/fix-report-modal-pickup-3c94
```

## فایل تغییر یافته

- `src/app/pages/consultant-dashboard/consultant-dashboard.component.ts`
