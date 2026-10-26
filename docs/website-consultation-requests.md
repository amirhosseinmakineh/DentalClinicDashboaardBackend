# Website consultation requests

## Source and branch audit

Backend default `main` is at `a4afd902643035887204fb0c461e8606990523c3`, but does not contain the current import coordinator, source enum or the named role workers. This change is based on `codex-lflotm` at `b2687688232f00755b813ba4391297db7c746300`; the backend PR must target that branch. Do not merge this branch directly into the older main implementation.

The actual clinic frontend is `amirhosseinmakineh/drsaeedMoghadamFront`, default branch `main`, reviewed at `0b14a02c5868b2417464149ff487188d39fd4fa2`. Its homepage previously acknowledged a request without sending it to an API. Existing same-origin `/api` configuration is retained.

## Existing import and allocation business

* `LeadImportCoordinator` runs Yektanet before AdminSheet. Yektanet reads the exported XLSX, normalizes mobiles and calls the existing serializable `AddNewLeadsIfPhoneDoesNotExistAsync` historical-phone check.
* AdminSheet stores raw rows in `AdminLeadSheets` / `AdminSheetLeads`. Pending status 0 rows become `LeadAssignment` records with `New`, `RealTime`, `RequiresThreeMinuteCall=true`; processed rows use status 1 and errors status 2. A per-row serializable transaction now protects its pre-existing duplicate check and row/result saves against concurrent imports or Website insertion.
* Website adds its graph directly through the same serializable repository insertion. It never calls a new allocator and does not need to wait for an external import.
* `LeadAssignmentSourceType` (`NewLeads` / `BurnedLeads`) and the state enum are unchanged. Existing `LeadSourceType` is reused: Yektanet=1, Clinic=2, AdminSheet=3, Website=4. No historical integer is renumbered or guessed.
* Eligibility remains nondeleted, realtime, new, unassigned, unpicked, no report, and no other record for the same phone with a submitted report. Existing burned-lead eligibility and reset rules are unchanged.
* Existing two-source alternation is retained: Website joins the existing non-Yektanet/AdminSheet candidate queue, using its existing created-time/id ordering. A Website pickup counts as the non-Yektanet turn. This is the only candidate-selection extension; consultant roles, capacity, availability, working hours, limits, notifications, pickup, deadlines and redispatch use existing services.
* All three role workers are notification/broadcast workers. Ownership is assigned by the existing SQL conditional `TryPickupLeadAsync` update with row/update locks; competing pickups have one winner. Concurrent workers can still produce overlapping broadcast notifications, as before; they cannot assign the same new lead twice.
* A duplicate website phone is not reset, reassigned, converted into a new lead or used to reveal an existing lead. Existing historical records, including deleted history under the shared Yektanet insertion rule, prevent insertion. The public conflict response is generic.

## APIs and data

`GET /api/consultation-requests/services` is anonymous and returns only id/title of active, nondeleted records in the existing `SecretarySaleServices` database catalog. Prices and rewards are not exposed. Existing reservation enums and marketing descriptions are not used as a fake catalog.

`POST /api/consultation-requests` is anonymous. Required header: `Idempotency-Key: <nonempty UUID>`. Body:

```json
{
  "fullName": "نام و نام خانوادگی",
  "phoneNumber": "+989123456789",
  "serviceIds": [1, 2],
  "description": "توضیحات اختیاری"
}
```

Names require at least three letters and at most 100 characters. Common Iranian mobile formats and Persian/Arabic digits normalize to one 09 identity. One to ten distinct active catalog services are supported. Notes are plain text, optional, at most 1000 characters, and Angular renders them as escaped text.

Successful response is `{ "saved": true, "message": "..." }` only after transaction commit. Identical key+normalized-payload retries replay that acknowledgement. Different payloads using the same key conflict. A filtered unique request-key index protects retries; a SHA-256 payload fingerprint is internal and is not returned in panel DTOs. Under competing SQL transactions a deadlock victim can return the existing generic 500 response; the client retains its key and inputs for a safe retry.

POST is limited to five requests per minute per observed remote address, no queue, and 16 KiB request size. `ConsultationRequests:PermitLimit` can explicitly tune the limit (1..1000); production default is five. No caller-controlled forwarding header is trusted. Behind an unconfigured reverse proxy all requests may share its address; deployment must review existing proxy address handling rather than blindly trusting X-Forwarded-For. No personal data is added to logs/errors.

Existing `/api/LeadAssignment`, `/api/Consultant/GetLeads` and `/api/Consultant/GetNewLeads` responses add `sourceType`, `requestedServices` (id/title), and `consultationDescription`. Lead-list requests accept an optional `SourceType` filter alongside the old filters. Existing created-time data is retained. Admin UI adds source filter/columns; consultant UI displays source, services, visitor notes and request time.

## Database upgrade

Prepared migration: `20261024171020_WebsiteConsultationRequests`.

* Nullable columns on `LeadAssignments`: `ConsultationRequestId`, `ConsultationRequestHash`, `ConsultationDescription`.
* Filtered unique index on non-null `ConsultationRequestId`.
* `LeadRequestedService` composite-key relationship to the existing lead and service catalog; no copied service names/catalog and no separate Website lead table.
* No delete, reassignment, source backfill, enum renumbering or destructive operation in Up.
* Guards require the current source column and service catalog; this upgrade intentionally does not bootstrap an older schema.

The pre-existing EF snapshot was stale and omitted several current modules. The snapshot/designer now represent the current model, but the migration Up was narrowed to consultation-only changes. Do not use the original unreviewed generated migration, which would recreate unrelated tables.

`docs/sql/2026-10-24-website-consultation.sql` is the reviewed idempotent upgrade script generated from the last pre-feature migration `20261013150000_AddPatientFinanceServiceUnitCounts`. Inspect actual migration history and schema before applying. In particular, the older `20261009090000_AddAdminLeadSheets.cs` has no discovery attribute in the base branch; do not assume a clean replay of the legacy migration chain bootstraps the current runtime schema. The new script fails if its current-schema prerequisites are missing.

Production database was not accessed or migrated. No merge, deploy, secret or connection-string change was performed. Apply the approved upgrade before deploying the API, then frontend, and verify actual production catalog/proxy configuration.

## Repeatable validation

Build backend: `dotnet build DentalDashboard.slnx -c Release`.

The new integration project has 15 input theory cases plus one multi-scenario SQL/HTTP workflow. Set `CLINIC_TEST_SQL_CONNECTION` to a disposable loopback SQL Server, then run:

```sh
dotnet test DentalDashboard.IntegrationTests/DentalDashboard.IntegrationTests.csproj --logger 'console;verbosity=detailed'
```

Tests reject non-loopback servers, create a random `ClinicWebsiteTests_...` database and delete only that database. Actual SQL Server, EF transactions, constraints, repository logic, HTTP, JWT authorization, candidate selection, all three allocation methods, pickup and panel API projections are exercised. An injected SQL trigger verifies rollback on a save failure. Default rate limiting is tested on an independent host.

Test-only XLSX input replaces the external Yektanet response, push delivery is captured locally, and working-time eligibility is made deterministic. No real campaign data is imported and no real consultant is notified. These fixtures are confined to the test project; final application code uses real services.

For cross-repository browser E2E, build the paired frontend and set `CLINIC_BROWSER_TEST_SCRIPT` to its absolute `scripts/test-consultation-browser.mjs` path before running the same workflow. Install Playwright Chromium; optionally set `CLINIC_CHROMIUM_EXECUTABLE` to an existing executable and `CLINIC_SCREENSHOT_DIR` to a temporary QA directory. The workflow starts its real HTTP server and invokes the browser against that server and real SQL database. It verifies mobile/desktop success, failed-network recovery, live validation, loading, retained inputs, identical retry keys, duplicate-click prevention and assigned-lead display in the consultant panel.

CI adds an isolated SQL Server integration job for pull requests. Tests do not deploy or migrate an application database.

## Changed backend files

* `.github/workflows/website-consultation-tests.yml`
* `DentalDashboard.ApplicationService.Contract/Requests/Lead/Queryies/GetLeadsQuery.cs`
* `DentalDashboard.ApplicationService.Contract/Responses/LeadResponse/LeadsAssignmentItemsResponse.cs`
* `DentalDashboard.ApplicationService/Handlers/QueryHandlers/Lead/GetLeadsAssignmentQueryHandler.cs`
* `DentalDashboard.ApplicationService/Handlers/QueryHandlers/Lead/GetNewLeadsQueryHandler.cs`
* `DentalDashboard.Domain/Enums/LeadSourceType.cs`
* `DentalDashboard.Domain/Models/LeadAssignment.cs`
* `DentalDashboard.Domain/Models/LeadRequestedService.cs`
* `DentalDashboard.Infrastracture/Configuration/LeadAssignmentConfiguration.cs`
* `DentalDashboard.Infrastracture/Configuration/LeadRequestedServiceConfiguration.cs`
* `DentalDashboard.Infrastracture/Migrations/20261024171020_WebsiteConsultationRequests.Designer.cs`
* `DentalDashboard.Infrastracture/Migrations/20261024171020_WebsiteConsultationRequests.cs`
* `DentalDashboard.Infrastracture/Migrations/DentalContextModelSnapshot.cs`
* `DentalDashboard.Infrastracture/Repository/LeadAssignmentRepository.cs`
* `DentalDashboard.IntegrationTests/ConsultationInputTests.cs`
* `DentalDashboard.IntegrationTests/ConsultationWorkflowTests.cs`
* `DentalDashboard.IntegrationTests/DentalDashboard.IntegrationTests.csproj`
* `DentalDashboard.LeadManagement/Application/AdminLeadSheetService.cs`
* `DentalDashboard.LeadManagement/Application/Consultation/ConsultationRequest.cs`
* `DentalDashboard.slnx`
* `DentalDashboard/Controllers/ConsultationRequestsController.cs`
* `DentalDashboard/Program.cs`
* `docs/sql/2026-10-24-website-consultation.sql`
* `docs/website-consultation-requests.md`

Browser validation also exercises the authenticated admin table, Website source filter, requested service titles and visitor notes against the real paired API.
