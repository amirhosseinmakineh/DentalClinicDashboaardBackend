# Website consultation requests

Backend base: `codex-lflotm` (`b268768`), explicitly approved after discovering that default `main` lacks the current import coordinator and source model. Frontend: `amirhosseinmakineh/drsaeedMoghadamFront`, base `main` (`0b14a02`). Both feature branches: `feature/website-consultation-requests`.

## API and database

- `GET /api/consultation-requests/services`: active, nondeleted `SecretarySaleServices`, exposes only ID/title. Existing reservation services are an enum; the existing database-backed service catalog is reused without copying records. One service per request keeps the form short and stores a nullable foreign key on the lead.
- `POST /api/consultation-requests`: `{fullName, phoneNumber, serviceId, notes, requestId}`. Anonymous, validates server-side, normalizes Iranian mobile numbers and Persian/Arabic digits, limits notes to 1000 characters, deduplicates through the existing serializable repository insertion method. `requestId` is a UUID with a unique filtered database index. Exact retries return success only when that payload already committed. Changed payloads using the same key, and existing phone numbers using a new key, return 409. Database failures return 503 without SQL details or submitted values.
- POST limiter: 5 attempts per 5 minutes per connection IP, no queue, returns 429. Deployment must retain the existing trusted reverse-proxy configuration; arbitrary X-Forwarded-For headers are not trusted by this change.
- Existing admin and consultant lead list responses gain `sourceType`, `consultationServiceId`, `consultationServiceTitle`, `consultationNotes`. Existing admin/consultant lead filters gain optional `SourceType`.
- Additive migration `20261024171233_AddWebsiteConsultationMetadata`: three nullable columns on `LeadAssignments` (`ConsultationServiceId`, `ConsultationNotes`, `ConsultationRequestId`), FK with restricted delete, service index and unique filtered request index. No additional lead/service tables. Existing `SourceType` is retained: Yektanet=1, Clinic=2, AdminSheet=3, Website=4. `LeadAssignmentSourceType` (new/burned) is unchanged. No guessed historic backfill or historical source rewrites.
- The prior snapshot omitted models/columns already introduced by earlier migrations. EF regenerated the snapshot/designer from the current model; the migration body deliberately contains only the three additive consultation fields and indexes/FK. Review the migration, rather than replaying the entire pre-existing migration history against an established database.

## Existing allocation flow

Yektanet is fetched/parsed by `LeadAssignmentService.AddLeadsAsync` and inserted with `AddNewLeadsIfPhoneDoesNotExistAsync`. `LeadImportCoordinator` processes Yektanet before pending admin-sheet rows. Admin sheets persist `AdminSheetLead`, then `ProcessPendingAsync` creates `LeadAssignment` as `RealTime`, `New`, requiring the existing call window. The admin import transaction now includes its duplicate check, lead insert and processed-row update to serialize competing writers.

Website writes directly into that same lead queue with the same new/realtime flags; it does not pick a consultant. The existing Yektanet inbound selection predicate also includes Website, preserving the existing inbound/admin rotation, ordering, redispatch interval, eligibility and burn rules. Website counts as an inbound pickup for that rotation. The same test/seller/top-seller service methods broadcast it; existing conditional SQL pickup updates choose one winner. Capacity, work hours, call deadlines, expiry, reassignment and notification implementations are retained.

The browser form uses database service IDs, preserves fields on errors, blocks double submission, keeps a retry UUID for unchanged payloads, waits for backend `success`, and uses a 20-second timeout. Admin tables show/filter origin and show service/notes; consultant lead cards show these fields with Angular interpolation.

## Verification and limits

Production was not contacted, migrated, merged or deployed. All persistence tests own randomly named disposable SQL Server databases. Synthetic service records/users exist only in tests. Yektanet tests feed a generated XLSX through the real parser/importer/coordinator; the live vendor export and real push subscriptions are not invoked. Controlled daytime eligibility is used in allocation fixtures so CI does not depend on the wall clock. The browser test runs the actual built homepage against the real consultation controller/repository/SQL Server; after submission it uses existing allocation and pickup services and reads the existing consultant query response. Authenticated admin/consultant screens are extended in source; their full signed-in browser journeys and live push delivery are not covered by this local fixture.

Build: `.NET 10` backend and `NG_CLI_ANALYTICS=false npm run build:local` frontend (including PWA config validation). Existing nullable/package vulnerability warnings remain unchanged.

Run SQL tests against an isolated SQL Server instance:

```sh
export CONSULTATION_TEST_SQL='<isolated test SQL Server connection>'
dotnet test DentalDashboard.Consultation.Tests
```

The test runner always creates and removes its own `ConsultationTests_<uuid>` database. CI supplies a disposable SQL Server container. Without browser variables the browser fact is reported as skipped, not passed.

For the full browser test, build the sibling frontend, install Playwright/Chromium in a test-only dependency directory, then set:

```sh
export CONSULTATION_FRONTEND_DIST='<absolute frontend>/dist/demo/browser'
export CONSULTATION_BROWSER_SCRIPT='<absolute frontend>/scripts/test-consultation.mjs'
export PLAYWRIGHT_MODULE='<absolute test dependency directory>/node_modules/playwright'
dotnet test DentalDashboard.Consultation.Tests
```

Browser cases use 1440x1000 desktop and 390x844 mobile, real API services/SQL persistence, double clicks, a deliberately aborted connection, input preservation and retry. External page requests are blocked in the test; service workers are disabled in the fixture to make connection-failure injection deterministic.

Local verification on 2026-10-24: **13/13 tests passed**, including the real desktop/mobile browser -> HTTP -> SQL Server -> existing allocation/pickup -> consultant-query flow. Other passing cases: existing Yektanet XLSX parser/coordinator and admin import, duplicate/replay protection, invalid phone/service, save failure, rate limiting, concurrent background allocation steps and competing pickups, capacity rejection, additive migration preserving an existing lead. Frontend production build and backend build passed.

## Backend files

- `docs/website-consultations.md`
- `.github/workflows/consultation-tests.yml`
- `DentalDashboard.ApplicationService.Contract/Requests/Lead/Queryies/GetLeadsQuery.cs`
- `DentalDashboard.ApplicationService.Contract/Responses/LeadResponse/LeadsAssignmentItemsResponse.cs`
- `DentalDashboard.ApplicationService/Handlers/QueryHandlers/Lead/GetLeadsAssignmentQueryHandler.cs`
- `DentalDashboard.ApplicationService/Handlers/QueryHandlers/Lead/GetNewLeadsQueryHandler.cs`
- `DentalDashboard.ApplicationService/Services/LeadAssignmentService.cs`
- `DentalDashboard.Consultation.Tests/ConsultationTests.cs`
- `DentalDashboard.Consultation.Tests/DentalDashboard.Consultation.Tests.csproj`
- `DentalDashboard.Domain/Enums/LeadSourceType.cs`
- `DentalDashboard.Domain/Models/LeadAssignment.cs`
- `DentalDashboard.Infrastracture/Configuration/LeadAssignmentConfiguration.cs`
- `DentalDashboard.Infrastracture/Migrations/20261024171233_AddWebsiteConsultationMetadata.Designer.cs`
- `DentalDashboard.Infrastracture/Migrations/20261024171233_AddWebsiteConsultationMetadata.cs`
- `DentalDashboard.Infrastracture/Migrations/DentalContextModelSnapshot.cs`
- `DentalDashboard.Infrastracture/Repository/LeadAssignmentRepository.cs`
- `DentalDashboard.LeadManagement/Application/AdminLeadSheetService.cs`
- `DentalDashboard.slnx`
- `DentalDashboard/Controllers/ConsultationRateLimiting.cs`
- `DentalDashboard/Controllers/ConsultationRequestsController.cs`
- `DentalDashboard/Program.cs`

## Frontend files

- `scripts/test-consultation.mjs`
- `src/app/core/admin/admin-dashboard.service.ts`
- `src/app/core/consultant/consultant-dashboard.service.ts`
- `src/app/pages/admin-dashboard/admin-leads-table.component.html`
- `src/app/pages/admin-dashboard/admin-leads-table.component.ts`
- `src/app/pages/consultant-dashboard/consultant-dashboard.component.html`
- `src/app/pages/home/home.component.html`
- `src/app/pages/home/home.component.ts`
