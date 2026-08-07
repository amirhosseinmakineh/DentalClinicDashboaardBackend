# TopSeller (Gold) workflow analysis

## Current distribution flow

```text
AddLeadBackgroundService -> ILeadAssignmentService.AddLeadsAsync -> realtime queue
LeadAssignmentBackgroundService -> ILeadAssignmentService.AssignRealTimeLeadsAsync
  -> existing consultant eligibility query -> daily-limit service
  -> realtime push notification -> existing atomic TryPickupLeadAsync endpoint
TestConsultantBackgroundService -> TEST burned broadcast + TEST evaluation commands
SellerLeadDistributionBackgroundService -> Seller burned broadcast + Seller evaluation commands
```

Burned records are existing `LeadAssignments` with `IsDeleted = 1`; both TEST and
Seller use the existing push/pickup pipeline. Successful patients are reservations
approved by the secretary after the consultant reports attendance, deduplicated by
lead assignment. Atomic pickup is the existing conditional SQL update with update/range
locks.

## Reusable components and extension points

- `ConsultantLevel.TopSeller` already represents Gold; no new role is required.
- `ILeadAssignmentService.AssignRealTimeLeadsAsync` remains the only realtime source.
- `TryPickupLeadAsync` remains the assignment/concurrency boundary.
- `IConsultantProfileService.SetConsultantLevelAsync` remains the transition flow and
  initializes Seller after a TopSeller downgrade.
- `User.IsActive`, plus profile availability/online state, controls login/dashboard and
  lead eligibility. TEST failure currently sets those fields in its handler; this will
  move behind an idempotent application service operation.
- No reward/payment module exists. The TopSeller domain decision and lifecycle fields
  will persist `None`, `Level1`, or `Level2` for future reward execution without adding
  payment behavior.

## Dynamic-limit approach

A single framework-free domain resolver will map consultant level to allowed lead types
and limits: TEST `0/20`, Seller `10/30`, TopSeller `30/0`. Application eligibility and
the atomic infrastructure update will consume that policy, so a level transition takes
effect without updating a stored per-user limit.

## Weekly convention

TopSeller weeks are deterministic rolling seven-full-day windows starting at
`TopSellerStartedAt` (Iran-local date boundary). Evaluation begins only at the next
midnight after all seven days. Each completed period is atomically marked to prevent
duplicate evaluation/reward decisions.

## Planned changes

- Domain: role distribution policy/resolver, TopSeller strategy, reward enum, tests.
- Contract/Application: active TopSeller query; role-based realtime and weekly
  evaluation commands/handlers; idempotent consultant deactivation service.
- Infrastructure: role-specific counts, policy-driven atomic limits, TopSeller lifecycle
  query/claim, migration and indexes.
- Host: TopSeller evaluator and CQRS role-based realtime worker. The legacy direct
  `LeadAssignmentBackgroundService` registration in `Program.cs` is the execution point
  that will be commented out, while its source remains intact.
- SQL: a manual script will target `ConsultantProfiles.ConsultantLevel` (`TopSeller = 3`)
  joined to active, non-deleted `Users` having a non-deleted `UserRoles` row whose
  `Roles.RoleName = 'Consultant'`; authorization roles are not changed.
