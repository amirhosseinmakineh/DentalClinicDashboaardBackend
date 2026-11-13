# Patient file completion

The secretary creates the first-page intake immediately. Page two is optional
and can be entered later from Edit after the consultant returns the paper form.
No consultation field is required. An explicit `ConsultationCompleted` flag
records the secretary's confirmation; it is not inferred from a nonempty chart.

The patient-file list shows these mutually exclusive stages:

1. Waiting for consultation.
2. Complete file (opens the finance modal).
3. May work proceed? (opens the financial review modal).
4. Services authorized.

## Financial receipt

`POST /api/secretary/patient-files/{id}/complete-finance` creates a financial
case in the existing accounting tables and links it directly to the patient
file through `PatientFileWorkflows`. It never writes an accounting payment
from the advisory treatment rows on the paper form.

- Cash requires full payment and no cheques or notes.
- Installments can combine same-day cheques, dated installment cheques, and
  promissory notes. Optional cash received plus all documents must exactly
  cover the treatment total.
- Same-day cheques are pending, not paid. Their due date is the current Iran
  date. Other documents require an explicit due date of today or later.
- Due dates are converted from an Iran calendar date to UTC at the beginning
  of that Iran day. They remain visible in the existing commitment reports.
- All displayed amounts are toman. No existing stored amount is rescaled.
- A serializable transaction prevents a second completion for an already
  linked, non-cancelled financial case.

`POST /api/secretary/patient-files/{id}/approve-service` is a separate manual
confirmation. The server checks consultation completion, a valid linked case,
exact payment/document coverage, and absence of unpaid debts. The approving
user, UTC timestamp, and a fingerprint of the reviewed financial data are
stored. Financial changes through the existing accounting module invalidate
the displayed approval. Changing consultation treatments/chart or its completion
flag clears approval and requires another review.

Existing patients are not automatically declared consulted or financially
approved. Existing financial cases remain unchanged and viewable. The modal
offers them for reuse through `POST /api/secretary/patient-files/{id}/link-finance`
instead of recording the same receipts twice. A separate new treatment requires
an explicit acknowledgement when an earlier case already exists.

## Database rollout

Apply migration `20261113125109_AddPatientFileWorkflow` after the existing
`20261112180000_AddPatientPaperRecords` migration. It only adds the workflow
table and does not modify existing financial data. The model snapshot also
repairs previously stale metadata for PatientPaperRecords and the existing
PatientFinancialCases.OpgImageUrl column; this migration does not recreate
those objects or drop ConsultantProfiles columns.

Generate/review the deployment SQL or run the existing deployment migration
process against the intended database. No database migration is run by these
source changes. Restart the backend after migration; publish the matching
frontend and backend together.

## Verification

Run `dotnet test DentalDashboard.Consultation.Tests -c Release --filter
"FullyQualifiedName~PatientFileWorkflowTests|FullyQualifiedName~ConsultationUnitTests"`
and `npx ng build --configuration development` in the frontend repository.
Frontend receipt/payload checks run with
`node scripts/validate-patient-file-completion.mjs`.
