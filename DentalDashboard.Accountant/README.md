# DentalDashboard.Accountant

ماژول مستقل حسابداری سیستم است. این پروژه یک **Class Library مستقل** است و به هیچ‌یک از پروژه‌های Web، ApplicationService یا Infrastructure میزبان وابسته نیست.

## ساختار

- `Api`: کنترلرهای حسابداری و مالی بیمار.
- `Application/Contracts`: Command، Query، DTO و قرارداد سرویس‌ها.
- `Application/Handlers`: Command/Query Handlerهای CQRS.
- `Application/Validators`: اعتبارسنجی ورودی‌های حسابداری.
- `Application/Services`: سرویس‌های application-level مانند تولید رسید.
- `Domain`: Entity، Enum و قرارداد Repositoryهای ماژول.
- `Infrastructure`: EF Configuration، Repository و migrationهای متعلق به ماژول.

## اتصال به میزبان

- `AddAccountantApplicationServices()` سرویس، Handler و Validatorهای assembly ماژول را ثبت می‌کند.
- `AddAccountantInfrastructure()` Repositoryهای ماژول را ثبت می‌کند.
- میزبان، `DentalContext` را با قرارداد `DbContext` در DI ارائه می‌دهد؛ بنابراین Repositoryهای ماژول به Context اختصاصی میزبان وابسته نیستند.
- میزبان assembly ماژول را به MVC Application Parts اضافه می‌کند تا Controllerها discover شوند.
- `DentalContext`، EF Configurationهای assembly ماژول را هنگام ساخت model اعمال می‌کند.
- migrationها از نظر فیزیکی داخل ماژول هستند، اما به دلیل نیاز migration tooling به Context میزبان، توسط پروژه Infrastructure به‌صورت linked compile می‌شوند.

routeهای قدیمی `/api/secretary/...` فقط alias سازگاری هستند. API مستقل ماژول از `/api/accountant/...` در دسترس است.
