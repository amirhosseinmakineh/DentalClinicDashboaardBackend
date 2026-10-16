# Framework licensing architecture

The Framework package contains licensing contracts only. Enforcement is intentionally
not enabled in the backend during this migration.

## Integration points

- `ILicenseValidator` verifies a signed license payload and its product, installation,
  and expiration claims.
- `ILicenseStateStore` stores the last accepted license for the installation. A backend
  can use this cache during a temporary license service outage according to its policy.
- `LicenseInstallation` identifies the installation without coupling the Framework to a
  database or a clinic or patient model.

The future administration system can activate, deactivate, revoke, expire, and bind
licenses to installations. The backend should validate a signed license at startup and
at a bounded refresh interval, rather than call the licensing service on every request.
An outage policy should be explicit: use a still-valid cached license for a short grace
period, then enter a controlled read-only or unavailable state chosen by the product
owner. License expiry must not delete patient, clinic, or other application records.

Signing keys and service credentials must remain outside source control and application
configuration committed to this repository. The package does not contain a license
server, signing implementation, or production enforcement switch.

## Technical limitation

A customer who controls the server and possesses the compiled binaries can inspect or
patch local license checks. Signed licenses reduce accidental or unauthorized issuance;
they cannot provide tamper-proof execution on a customer-controlled host.
