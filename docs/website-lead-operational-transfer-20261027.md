# Website Lead transfer to the operational backend

Target base: `codex-lflotm` at `0bd548fea0f17b587fdde01fa071f391c60d05e7`.
Reference: `main` at `e80ebb37ee7c63d32bb23efa02362de577e8df46`.
Feature branch: `feature/website-lead-operational-transfer-20261027`.

## Contract and allocation

`POST /api/consultation-requests` anonymously accepts exactly this DTO:

```json
{"fullName":"نام و نام خانوادگی","phoneNumber":"09123456789","requestId":"frontend-generated UUID"}
```

There is no service catalog lookup, service requirement, notes input, default service or synthetic service. The public services endpoint is not transferred. Service/notes metadata columns from the original additive migration remain **nullable and unpopulated** by this endpoint, preserving the original migration identity/schema for environments which may have applied it. Their nullable response fields are retained for compatibility; they do not add form inputs.

Persian/Arabic digits and Iranian mobile prefixes are normalized. Names, numbers and nonempty UUIDs are validated. Exact retries succeed without reinsertion; changed payloads reusing a UUID and existing phone numbers with a fresh UUID return 409. SQL write errors return 503. Five POST attempts per connection IP per five minutes are allowed. No arbitrary forwarded IP headers are trusted.

The existing repository's serializable phone-deduplication operation inserts a `LeadAssignment` with `SourceType=Website (4)`, `AssignmentType=RealTime`, `State=New`, `RequiresThreeMinuteCall=true` and no consultant. Existing background workers broadcast and existing atomic pickup chooses the consultant, setting the three-minute deadline. Website joins the existing inbound Yektanet slot; a Website pickup rotates the next preference to AdminSheet. There is no second allocation service or worker. Existing source values, quotas, capacity, work window, burn/requeue, pickup and reservation business remain in place.

## Commit-by-commit review

| Main-only commit | Decision |
|---|---|
| `1fd230d` | Reservation-report merge, excluded. |
| `ec25a28` | Reservation-report query change, excluded. |
| `34c3ff9` | Reservation-report reconciliation merge, excluded. |
| `72d5387` | Reservation-report merge, excluded. |
| `231a3ec` | Secretary reservation actions, excluded. |
| `a4afd90` | Secretary reservation actions merge, excluded. |
| `9eb15af` | Website implementation examined file by file; selectively applied, contract/tests adapted below. |
| `df1c3ac` | Unrelated reservation reconciliation changes, excluded in full. |
| `e80ebb3` | Merge commit, never merged/cherry-picked. |

The 9eb15af patch was applied with migration/snapshot and old documentation excluded. Model metadata was then scaffolded from the actual target model. The only textual conflict was the middleware insertion in `Program.cs`; it was resolved by adding `UseRateLimiter` and preserving target authentication/authorization order and all CORS origins.

## Files transferred or adapted

- `.github/workflows/consultation-tests.yml`: SQL Server CI, solution build and all tests, push on this feature branch to verify before PR creation; no deploy job.
- `DentalDashboard.ApplicationService.Contract/Requests/Lead/Queryies/GetLeadsQuery.cs`: optional source filter on existing consultant/admin queries.
- `DentalDashboard.ApplicationService.Contract/Responses/LeadResponse/LeadsAssignmentItemsResponse.cs`: origin and nullable metadata response fields.
- `DentalDashboard.ApplicationService/Handlers/QueryHandlers/Lead/GetLeadsAssignmentQueryHandler.cs`: source filter/projection in both consultant and admin handlers.
- `DentalDashboard.ApplicationService/Handlers/QueryHandlers/Lead/GetNewLeadsQueryHandler.cs`: same extension for current new-leads query; existing paging/index logic retained.
- `DentalDashboard.ApplicationService/Services/LeadAssignmentService.cs`: Website treated as inbound for existing source rotation; other business retained.
- `DentalDashboard.Domain/Enums/LeadSourceType.cs`: Website=4, existing values unchanged.
- `DentalDashboard.Domain/Models/LeadAssignment.cs`: nullable consultation metadata.
- `DentalDashboard.Infrastracture/Configuration/LeadAssignmentConfiguration.cs`: nullable service relationship and unique filtered request UUID index; operational active-lead index retained.
- `DentalDashboard.Infrastracture/Repository/LeadAssignmentRepository.cs`: Website included in inbound dispatch and last-source selection; existing deduplication and pickup SQL retained.
- `DentalDashboard.LeadManagement/Application/AdminLeadSheetService.cs`: main's serializable transaction around duplicate check, insert and processed-row update, preventing competing Website/admin imports from bypassing checks.
- `DentalDashboard/Controllers/ConsultationRequestsController.cs`: adapted to name/phone/requestId only, no services endpoint or service/notes validation/writes.
- `DentalDashboard/Controllers/ConsultationRateLimiting.cs`: existing limiter.
- `DentalDashboard/Program.cs`: limiter registration and middleware only.
- `DentalDashboard.slnx`: test project registration only.
- `DentalDashboard.Consultation.Tests/DentalDashboard.Consultation.Tests.csproj`: existing test packages/project reference.
- `DentalDashboard.Consultation.Tests/ConsultationTests.cs`: real SQL/HTTP tests adapted to service-free requests; original importer, allocation, concurrency, capacity, failure and migration tests; authenticated consultant read/ownership, three-minute expiry, source selection and target reservation query regressions added. Browser/frontend test removed from this backend-only transfer.
- `DentalDashboard.Consultation.Tests/ConsultationUnitTests.cs`: new DTO/source-value, early validation and actual snapshot/model equivalence tests.
- `DentalDashboard.Infrastracture/Migrations/20261024171233_AddWebsiteConsultationMetadata.cs`: original additive body retained.
- `DentalDashboard.Infrastracture/Migrations/20261024171233_AddWebsiteConsultationMetadata.Designer.cs`: regenerated from target model, not copied from main.
- `DentalDashboard.Infrastracture/Migrations/DentalContextModelSnapshot.cs`: regenerated from target model, preserving active-lead index and reflecting previously omitted existing mappings.
- `docs/website-lead-operational-transfer-20261027.md`: this audit.

## Explicitly excluded changes

No changes to `SecretaryReservationItemResponse`, `GetSecretaryReservationsQueryHandler`, `ReservationController`, `DailyReservationsReportService`, `SecretaryDashboardService`, or `ReservationOperationsConfiguration` were transferred. The main-only `20260816120000_AlignReservationContactNoteLength` migration was not transferred: it depends on reservation tables/entities not present in the operational target. Old `docs/website-consultations.md` describes a required service and frontend changes and was not copied. No frontend files or unrelated feature-branch changes were imported.

## Migration status

The only new migration adds three nullable columns to `LeadAssignments`, a nullable service FK with restricted deletion, service index and a unique filtered UUID index. It neither recreates nor removes existing tables/columns, infers historical origins nor writes any service data. All pre-existing migrations, including `20261026150000_AddConsultantActiveLeadListIndex`, are preserved byte-for-byte.

The target snapshot was already stale (omitted existing AdminSheet, financial and other models introduced by prior migrations). Regeneration therefore produces a larger snapshot diff than the migration body. A Unit Test compares the regenerated snapshot with EF's actual target design model and requires zero differences. Snapshot reconciliation does not execute those historical schema changes.

The pre-existing history contains both `CreateDatabase` and later `InitialCreate` migrations creating overlapping tables. This task does not reset that history. SQL migration tests start from a disposable database representing the current target schema, apply the additive Website down/up operations, and verify existing lead/source preservation. This is an upgrade test, **not evidence that replaying the entire legacy migration chain on an empty database succeeds**. No production database or credentials are used.

## Validation

- Local backend solution Release build: passed, 0 errors (pre-existing nullable/package warnings).
- Local Unit Tests: 5 passed, 0 failed.
- Local SQL Server: engine initialization failed in this execution host; no SQL integration result is inferred from the build.
- Feature-branch SQL CI: pending. Runs SQL persistence/HTTP, deduplication/replay, real Yektanet workbook/coordinator, AdminSheet, allocation/pickup concurrency, capacity, consultant ownership/filter reads, call deadline/expiry, reservation projection/filters/access, and additive migration preservation.
- Production migration, merge and deployment: not performed.
