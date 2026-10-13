# Additional patient finance documents

This feature attaches up to 20 JPEG or PNG images (up to 5 MiB each) to a patient financial case.

## Architecture

- Domain entity: `PatientFinancialDocument`
- EF Core migration: `20261013100000_AddPatientFinancialDocuments`
- Repository: `IPatientFinanceRepository.Documents` and `PatientFinanceRepository`
- CQRS requests and handlers: `PatientFinancialDocumentContracts` and `PatientFinancialDocumentHandlers`
- Controller: `PatientFinanceController` (Admin/Secretary roles)
- Storage: `IFileStorageService` and `FileStorageService`

## HTTP routes

All routes are under `/api/secretary/patient-financial-cases/{id}` and require Admin or Secretary authorization.

- `GET /additional-documents`: list metadata, no public storage paths
- `POST /additional-documents`: multipart form with `file` and optional `description`
- `GET /additional-documents/{documentId}/image`: authenticated JPEG/PNG download
- `DELETE /additional-documents/{documentId}`: soft-delete metadata and remove image data

## Required production setup

**Configure `PATIENT_DOCUMENTS_STORAGE_ROOT` to a persistent, writable mounted volume on the backend container.** Do not rely on ephemeral container filesystems. In clustered deployments, every instance must share the same volume or replace the implementation with an object-storage provider.

Run EF Core migrations before enabling the feature. The existing guarantee-document upload and deletion endpoints have not been changed.

## Validation

Backend enforces non-empty image data, a 5 MiB size limit, JPEG/PNG signatures and a 20-image-per-case limit. Image downloads require authentication and verify both financial-case and document IDs.
