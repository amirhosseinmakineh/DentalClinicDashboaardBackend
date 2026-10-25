# راهنمای فرانت‌اند رسید تراکنش مالی

## تغییرات مدل تراکنش

فیلدهای `trackingNumber` (شماره پیگیری) و `receiptUrl` (نشانی رسید) از بدنه ثبت تراکنش و پاسخ‌های لیست/جزئیات حذف شده‌اند. فرانت‌اند نباید این دو فیلد را ارسال، نمایش یا جست‌وجو کند.

## API صدور رسید

```http
GET /api/secretary/account/financial-transactions/{id}/receipt
Authorization: Bearer <access-token>
Accept: text/html
```

- `id` شناسه عددی و مثبت تراکنش ثبت‌شده است.
- پاسخ موفق `200` یک فایل HTML مستقل، واکنش‌گرا، راست‌چین و مناسب چاپ است.
- نام فایل از هدر `Content-Disposition` قابل دریافت است.
- پاسخ `400` یعنی شناسه نامعتبر و پاسخ `404` یعنی تراکنش وجود ندارد یا حذف شده است.
- رسید در دیتابیس ذخیره نمی‌شود؛ در هر درخواست از آخرین اطلاعات همان تراکنش ساخته می‌شود.

## نمونه دریافت به‌صورت Blob

```ts
async function fetchTransactionReceipt(transactionId: number): Promise<Blob> {
  const response = await fetch(
    `/api/secretary/account/financial-transactions/${transactionId}/receipt`,
    {
      method: "GET",
      headers: {
        Authorization: `Bearer ${accessToken}`,
        Accept: "text/html",
      },
    },
  );

  if (!response.ok) {
    throw new Error("دریافت رسید تراکنش ناموفق بود");
  }

  return response.blob();
}
```

هنگام کلیک روی دکمه «صدور رسید»، تا پایان درخواست دکمه را غیرفعال و loading نمایش دهید. Blob را فقط بعد از پاسخ موفق بسازید و URL موقت را پس از استفاده با `URL.revokeObjectURL` آزاد کنید.

## اشتراک‌گذاری و دانلود اجباری

دکمه رسید باید هر دو قابلیت **اشتراک‌گذاری** و **دانلود** را داشته باشد. Web Share API در همه مرورگرها یا روی HTTP در دسترس نیست؛ بنابراین دانلود، fallback الزامی است.

```ts
async function shareOrDownloadReceipt(transactionId: number): Promise<void> {
  const blob = await fetchTransactionReceipt(transactionId);
  const fileName = `financial-transaction-receipt-${transactionId}.html`;
  const file = new File([blob], fileName, { type: "text/html;charset=utf-8" });

  if (navigator.share && navigator.canShare?.({ files: [file] })) {
    await navigator.share({
      title: `رسید تراکنش ${transactionId}`,
      text: "رسید تراکنش مالی",
      files: [file],
    });
    return;
  }

  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = fileName;
  link.click();
  URL.revokeObjectURL(url);
}
```

لغو پنجره Share توسط کاربر (`AbortError`) خطای سامانه محسوب نمی‌شود و نباید toast خطا نمایش داده شود. برای دکمه مستقل «دانلود»، همیشه بخش fallback بالا اجرا شود.

## نمایش و چاپ رسید

برای پیش‌نمایش، Blob را در پنجره جدید باز کنید. چون endpoint نیازمند توکن است، URL خود API را مستقیماً در `window.open` قرار ندهید؛ ابتدا با `fetch` فایل را دریافت کنید.

```ts
const blob = await fetchTransactionReceipt(transactionId);
const url = URL.createObjectURL(blob);
const receiptWindow = window.open(url, "_blank", "noopener,noreferrer");

if (!receiptWindow) {
  URL.revokeObjectURL(url);
  throw new Error("مرورگر اجازه باز کردن پیش‌نمایش را نداد");
}

setTimeout(() => URL.revokeObjectURL(url), 60_000);
```

کاربر در پیش‌نمایش می‌تواند از فرمان Print مرورگر، رسید را چاپ یا به PDF تبدیل کند.

## چک‌لیست تغییر فرانت‌اند

1. ورودی‌های «شماره پیگیری» و «نشانی رسید» را از فرم ثبت تراکنش حذف کنید.
2. ستون‌ها و مقادیر متناظر را از لیست و جزئیات تراکنش حذف کنید.
3. `trackingNumber` و `receiptUrl` را از type/interface و payload حذف کنید.
4. برای هر تراکنش دکمه «صدور رسید» اضافه کنید و endpoint جدید را فقط هنگام کلیک فراخوانی کنید.
5. loading، جلوگیری از کلیک تکراری، مدیریت `400`، `401` و `404` و پیام خطای عمومی را پیاده کنید.
6. امکان پیش‌نمایش/چاپ، اشتراک‌گذاری فایل و fallback دانلود را تست کنید.
7. Object URLهای موقت را آزاد کنید تا در استفاده مکرر حافظه مرورگر اشغال نشود.
