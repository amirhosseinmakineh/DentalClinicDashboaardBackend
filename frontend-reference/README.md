# Frontend snapshot: Realtime lead pickup alerts

این پوشه کپی تغییرات فرانت برای PR لید آنلاین است.

## چرا PR فرانت غیرفعال است؟

برنچ `cursor/realtime-lead-pickup-alerts-fca0` روی ریپوی
`dentalClinickDashboard` push نشده (دسترسی bot فقط به بکند است).

## روش ۱ — اعمال patch (پیشنهادی)

```bash
git clone https://github.com/amirhosseinmakineh/dentalClinickDashboard.git
cd dentalClinickDashboard
git checkout -b cursor/realtime-lead-pickup-alerts-fca0
git apply --check ../DentalClinicDashboaardBackend/docs/patches/realtime-lead-pickup-frontend.patch
git apply ../DentalClinicDashboaardBackend/docs/patches/realtime-lead-pickup-frontend.patch
git add -A
git commit -m "Implement realtime lead pickup alerts with Web Push and in-app overlay"
git push -u origin cursor/realtime-lead-pickup-alerts-fca0
```

سپس PR بسازید:

https://github.com/amirhosseinmakineh/dentalClinickDashboard/compare/main...cursor/realtime-lead-pickup-alerts-fca0

## روش ۲ — کپی دستی فایل‌ها

فایل‌های این پوشه را روی پروژه Angular کپی کنید:

- `public/` → `public/`
- `src/` → `src/` (فایل‌های متناظر)

## فایل‌های کلیدی

- `public/sw.js` — Service Worker
- `src/app/services/web-push.service.ts`
- `src/app/services/realtime-lead-pickup.service.ts`
- `src/app/services/realtime-lead-alert.service.ts`
- `src/app/components/realtime-lead-alert/`
- `src/app/components/consultant-dashboard/` (تغییر یافته)

## وابستگی بکند

نیاز به برنچ بکند: `cursor/realtime-lead-frontend-support-fca0`
