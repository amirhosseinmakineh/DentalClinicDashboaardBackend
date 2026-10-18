# CI/CD با GitHub Actions و Liara

این پروژه با GitHub Actions ساخته و با CLI رسمی Liara منتشر می‌شود. Pull Request فقط CI را اجرا می‌کند؛ push به `stage` یا `production` پس از موفقیت build و ساخت Docker image، deploy را اجرا می‌کند.

## جریان انتشار

1. تغییرات اصلی روی `codex-lflotm` آماده می‌شوند.
2. همان commit با merge به `stage` منتقل می‌شود.
3. پس از اعتبارسنجی stage، همان commit با merge به `production` منتقل می‌شود.
4. workflow ابتدا restore/build و Docker build را اجرا می‌کند.
5. job انتشار با `@liara/cli` به اپ متناظر Liara deploy می‌کند.

## تنظیم GitHub

در GitHub برای هر دو Environment با نام دقیق `stage` و `production` این موارد را بساز:

### Secret

- `LIARA_API_TOKEN`: توکن API حساب Liara

### Variable

- `LIARA_BACKEND_APP`: نام اپ backend در همان Environment

رمز دیتابیس داخل سورس یا workflow قرار نمی‌گیرد. مقدار کامل زیر را در تنظیمات Runtime اپ backend در Liara ثبت کن:

```text
ASPNETCORE_ENVIRONMENT=Stage              # اپ stage
ASPNETCORE_ENVIRONMENT=Production         # اپ production
ASPNETCORE_HTTP_PORTS=8080
ConnectionStrings__DefaultConnection=Data Source=manaslu.liara.cloud,30635;Initial Catalog=myDB;User Id=sa;Password=<رمز>;Encrypt=False;
```

برنامه خارج از Development نبودن این متغیر را در startup رد می‌کند؛ بنابراین به connection string فایل‌های checked-in برنمی‌گردد.

## health check و پورت

Dockerfile پورت `8080` و endpoint سلامت `GET /healthz` را ارائه می‌دهد. در Liara پورت container را `8080` و health path را `/healthz` تنظیم کن. دامنه API production باید به اپ production و دامنه stage به اپ stage متصل باشد.

## عیب‌یابی

خطاهای رایج شامل نبودن `LIARA_API_TOKEN`، نبودن `LIARA_BACKEND_APP`، connection string اشتباه یا health check روی پورت نادرست است.

این workflow از الگوی رسمی Liara برای GitHub Actions استفاده می‌کند: نصب `@liara/cli` و اجرای `liara deploy --api-token`.
