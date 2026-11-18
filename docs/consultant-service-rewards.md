# Consultant rewards based on performed services

## Deployment

1. Run `docs/sql/2026-11-17-consultant-service-rewards.sql` manually in the clinic database before deploying the updated backend and frontend. The script adds only this feature's migration and records its migration ID. It is transactional and can be rerun after successful completion.
2. In Accounting → Clinic accounting → Consultant accounting → Service reward rates, configure and activate the intended rates. Rules start inactive; a service without an active rule has zero reward, explicitly shown in the secretary's preview.
3. Existing reviewed rewards and pending legacy rewards retain their previous calculation. Historical services are not automatically recalculated.

## Workflow

- An approved patient file and a reviewable reservation are required. New positive results are recorded through patient files so the old attendance endpoint cannot bypass chart and reward validation.
- The secretary selects only the teeth/services actually performed from the saved chart, specifies the doctor, previews the reward, and confirms the result.
- An existing linked recipient is retained. If absent, the secretary must select an active user with the Consultant role. The recipient is stored separately from the appointment owner. Within the same financial treatment, subsequent visits retain the first recipient.
- The server recalculates the preview inside a serializable transaction. A changed quote requires a fresh preview. Performed teeth, immutable calculation details, recipient, and the result are committed together.
- A unique financial-case/service/tooth index prevents the same service being rewarded again at another visit. A new treatment uses a different financial case.
- Per-unit, completed-block, proportional-block, and jaw-threshold rules are supported. Jaw identity uses chart IDs 1–16 (upper) and 17–32 (lower).
- The first execution of each service freezes its rule version for that financial treatment. This keeps partial-block carry and jaw thresholds consistent across visits. New rule versions apply to treatments that have not started that service; they do not reprice existing service reward snapshots.
- Only admin approval adds the frozen amount to the consultant wallet total. Rejection does not charge the wallet; new service rewards require a rejection reason, stored as review metadata alongside the unchanged calculation. Approval/rejection cannot be repeated.

## Validation

Calculator tests cover unit/block/jaw calculations, carry, proportional rounding, invalid teeth, immutable snapshots, frozen versions, recipient selection, and the unique execution constraint. Existing result tests cover doctor/date/permissions and duplicate submissions. Frontend scripts exercise recipient selection, quote invalidation, preserved reservation identity, and duplicate-click guards. No production database migration or live payment was executed during development.
