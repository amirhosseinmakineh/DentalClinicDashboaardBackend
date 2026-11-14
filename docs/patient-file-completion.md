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

Services and unit counts in the patient-file finance modal are read-only and
come exclusively from the saved dental chart. Both legacy single-service and
multi-service tooth entries are supported; a service counts once per tooth.
The server reads the chart inside the financial transaction and rejects empty
charts or submitted counts that differ from the saved chart. This applies to
new receipts, financial corrections and financial approval. Linking an earlier
invoice requires its service counts to match the chart.

The finance modal can open the patient-file editor and returns to finance with
fresh data after saving. A confirmed treatment plan can be revised before
finance exists. Revising an already financed chart synchronizes invoice service
counts without changing amounts, receipts, documents or reservation results,
and clears finance/profile/service approvals for another review. A financed
chart cannot be erased completely. This change adds no database columns.

`POST /api/secretary/patient-files/{id}/complete-finance` creates a financial
case in the existing accounting tables and links it directly to the patient
file through `PatientFileWorkflows`. It never writes an accounting payment
from the advisory treatment rows on the paper form.

- Cash requires full payment and no cheques or notes.
- Installments can combine same-day cheques, dated installment cheques, and
  guarantee promissory notes. Cash plus ordinary payment documents must not
  exceed treatment cost; pending guarantees must cover any remaining portion.
  New notes entered through patient-file completion are guarantees by default
  and may have a nominal amount above treatment cost (150M treatment / 300M
  guarantee is valid). Their face value never increases patient liability,
  paid amount or debt. Existing notes remain payment notes via IsGuarantee=false.
- Optional InstallmentDescription is stored in the existing case Notes column,
  returned when editing, and shown in financial review and dossier details.
  Guarantee notes cannot be marked paid/unpaid through ordinary document status
  endpoints and are excluded from the upcoming payment collection list.
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

The guarantee-note distinction needs the IsGuarantee bit column on
PatientPromissoryNotes. Apply `docs/sql/2026-11-14-promissory-note-guarantee.sql`
manually, or migration `20261114120000_AddPromissoryNoteGuarantee`, before running
the matching backend. Default false preserves existing notes. The manual SQL is
idempotent and no database change has been applied by the code task.

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


### Ordinary and guarantee cheques — 2026-11-14

- New ordinary cheques are installment payment documents, with a selected due date. Guarantee cheques use the existing PatientCheques.IsGuarantee column and never become automatic payments or nominal debts.
- Pending guarantee cheques remain editable alongside pending guarantee notes. Both guarantee types may exceed treatment cost without increasing the patient's liability. Existing cheque classifications are preserved.
- Admin due reports include ordinary and guarantee cheques, and a separate promissory-note report with the same Iran-day date filtering and pagination.
- Admins can explicitly record realized guarantee proceeds against registered overdue unpaid commitments. The serializable operation settles the corresponding debts and original documents, records the actual overdue debt amount against the guarantee, and prevents repeat collection or receipts exceeding the treatment balance/security. It does not automatically collect guarantees on their due date.
- No additional migration or SQL is required after the previously supplied PatientPromissoryNotes.IsGuarantee script.
