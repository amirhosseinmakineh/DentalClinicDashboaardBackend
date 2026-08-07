# Seller workflow implementation analysis

## Existing flow

```text
AddLeadBackgroundService -> ILeadAssignmentService -> LeadAssignmentRepository
LeadAssignmentBackgroundService -> realtime broadcast -> atomic TryPickupLeadAsync
TestConsultantBackgroundService -> Process/Evaluate commands -> domain TEST strategy
level transitions -> IConsultantProfileService.SetConsultantLevelAsync
```

`TryPickupLeadAsync` is the concurrency boundary: one conditional SQL update claims a
lead and uses update locks while checking the consultant's daily allocation. TEST
burned-lead broadcasts and normal realtime broadcasts consequently share the same
assignment history and pickup endpoint.

## Reusable components

- `ILeadAssignmentService` for the existing broadcast/notification pipeline.
- `ILeadAssignmentRepository.TryPickupLeadAsync` for atomic ownership and quotas.
- `IConsultantProfileService.SetConsultantLevelAsync` for all level transitions.
- Existing reservation attendance/secretary approval predicate for confirmed patients.
- CQRS dispatchers and the hosted-service scope/error-handling convention.

## Planned files

- Domain: add pure Seller context, policy, decision, and strategy; add unit tests.
- Contract/Application: add Seller queries, distribution/evaluation commands and handlers.
- Infrastructure: extend consultant/lead repositories, persist Seller lifecycle dates, and
  enforce both Seller quotas inside the atomic pickup statement.
- Host: add and register `SellerLeadDistributionBackgroundService`.

The normal realtime source remains unchanged. The Seller worker only adds the burned
lead broadcast and evaluation orchestration; assignment still occurs through the
existing pickup pipeline.
