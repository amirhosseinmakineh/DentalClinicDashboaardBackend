# Website lead and consultant dashboard synchronization — 2026-10-26

## Paired revisions

Frontend: `af4149647a7f86c311e5a414913fcca1841173c5`, PR #382, target lowercase `stage`.
Backend: this feature branch targets `main`; it retains Website changes already merged through PR #260 and incorporates the existing latest `codex-lflotm` changes through `0bd548f`. No allocation algorithm is introduced.

## Broken merge repaired

A real build of `main` (`e80ebb3`) failed because secretary reservation code referenced absent `ReservationRequestStatus`, `VisitResultStatus`, `FollowUpPriority`, `ReservationContactLog`, `ReservationNote` and `ReservationNotificationOutbox` models. Comparing with the common ancestor `b268768` and the current `codex-lflotm` showed unrelated/inconsistent reservation files introduced while reconciling Website intake.

The existing, consistent files are restored from `codex-lflotm`:

- `DentalDashboard.ApplicationService.Contract/Responses/ReservationResponse/SecretaryReservationItemResponse.cs`
- `DentalDashboard.ApplicationService/Handlers/QueryHandlers/Reservation/GetSecretaryReservationsQueryHandler.cs`
- `DentalDashboard/Controllers/ReservationController.cs`
- `DentalDashboard/Services/DailyReservationsReportService.cs`

Two unsupported merge fragments absent from both the common ancestor and current `codex-lflotm` are removed: `ReservationOperationsConfiguration.cs` and `SecretaryDashboardService.cs`. Their referenced entity/query types do not exist; these were not working features. Existing reservation commands, secretary operations, time confirmation, consultant updates, rewards and announcement support come from the existing current controller. No new models or guessed fields are added to satisfy stale code.

## Latest backend changes retained

The current branch's 22:00 workday end, active-lead filtered pagination index/migration and localhost CORS support are combined with Website intake/metadata. The EF snapshot includes the same index alongside consultation metadata. The frontend auto-absence boundary is aligned with the existing 22:00 rule. Neither source enum values nor capacity, deduplication, call deadlines, redistribution or notification algorithms change.

Database changes remain additive: the already-merged `AddWebsiteConsultationMetadata` migration and the existing `AddConsultantActiveLeadListIndex` migration from `codex-lflotm`. No production migration is run. Do not replay the historical migration chain indiscriminately against an established database.

## Verification

Local: full backend solution and consultation-test project build; frontend TypeScript check, 24 contract/preservation/form tests, and development/stage/production builds.

The expanded backend integration workflow checks out the exact frontend revision above, builds it, installs test-only Chromium, and runs the actual public controller, SQL persistence, shared allocation/pickup, authenticated consultant lead controller and consultant lead UI at desktop/mobile sizes. Its SQL Server is disposable. Existing duplicate, Yektanet/admin-sheet importer, concurrent pickup, capacity, invalid input, failure, rate-limit and migration-preservation tests remain.

The test-only authentication handler and `/test/pickup/{phone}` route exist only inside `DentalDashboard.Consultation.Tests`. Production `Program.cs` never registers either. Lead writes, service catalog and consultant lead reads are real; unrelated dashboard summary/broadcast/reservation views use explicit browser fixtures. No production user, database, push subscription or deployment is contacted.

The scratch host cannot initialize SQL Server; real persistence/browser success must be read from the CI run, not inferred from a passing build.
