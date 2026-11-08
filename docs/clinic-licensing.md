# Clinic license integration

This feature branch consumes the private `DentalDashboard.Framework` package version `1.1.0-rc.1`. Deploy the independent licensing server from the private Framework repository first. The Framework source remains outside this backend repository.

## Runtime configuration

Supply environment variables using the host's secret configuration:

| Variable | Purpose |
| --- | --- |
| `License__ServerUrl` | HTTPS origin of your licensing server |
| `License__InstallationId` | Installation ID issued in the management panel |
| `License__ProductId` | `dental-clinic`, matching the panel record |
| `License__Environment` | Exact environment in that record; defaults to the backend runtime environment name |
| `License__InstallationSecret` | Secret displayed at creation or rotation |
| `License__PublicKeys__clinic-2026-01` | Public PEM for that signing-key ID; preserve its newlines |
| `License__CachePath` | Writable file on persistent storage, e.g. `/data/license/license.json` |

Use the actual key ID instead of `clinic-2026-01` if different. Do not give a clinic the server's signing private key or admin key. Never commit installation secrets. Each replica of one clinic may use the same installation but should have its own writable cache file. Separate clinics and environments must have separate installations and secrets. No `Enabled=false` bypass is provided. Missing configuration prevents startup; unavailable/invalid licensing returns 503 on business requests.

HTTP is accepted only for loopback license servers when the backend runs in Development. Both server and clinic must use `Development` as the installation environment for that local scenario. Production requires HTTPS. Redirects are disabled for outbound license requests.

## Behavior

The first check runs before business workers start. Further checks run every five minutes. A signed lease is valid for at most 24 hours, bounded by the purchased expiration. Network failures retain only the remaining signed validity. A signed disabled state persists across restarts. Returning online and re-enabling the license restores access automatically at the next check.

All requests except the exact `/healthz`, `/readyz`, and `/api/license/status` paths are blocked with 503 when licensing denies access. CORS preflights are handled before the gate. The status endpoint exposes only validity, reason and lease deadline, never secrets. Health endpoints describe process/database health, not business license validity. Framework command/query dispatchers enforce the gate too. SignalR checks new connections and method invocations. Background assignment/import workers pause before new cycles. Already-running operations are not forcibly terminated.

Clinic data is never deleted or changed by license enforcement. The central service stores license metadata only. This change has not been merged into the production branch.

## Build and acceptance

Use the authenticated NuGet configuration described in `private-framework-build.md`, after the preview package has been published:

```bash
dotnet restore DentalDashboard.slnx --configfile NuGet.Private.Config
dotnet build DentalDashboard.slnx -c Release --no-restore
dotnet test tests/ClinicLicensing.Tests/ClinicLicensing.Tests.csproj -c Release --no-build --no-restore
docker build --secret id=nuget_config,src=NuGet.Private.Config -t dental-back:licensed .
```

On staging, issue an installation and verify normal access; disable it and wait one check interval; confirm API 503 and paused background cycles; re-enable and confirm recovery. Test network failure with an existing lease and a fresh installation separately. Preserve the cache volume across restarts. The Framework suite separately tests offline restart/expiry, tampering, product/environment binding, administrator CSRF/authentication, persistence, revocation and secret rotation.
