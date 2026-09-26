# ArvanCloud container deployment

## Build the image

Build from the repository root and publish an immutable tag (for example, the Git commit SHA):

```bash
docker build --platform linux/amd64 -t REGISTRY/dental-back:GIT_SHA .
docker push REGISTRY/dental-back:GIT_SHA
```

Create an ArvanCloud Container application from this image with container port `8080` and
exactly one replica. The application runs lead-assignment background workers, so increasing
the replica count is unsafe until distributed coordination is implemented.

## Required configuration

Store sensitive values as ArvanCloud Secrets and expose them as environment variables:

```dotenv
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_HTTP_PORTS=8080
TZ=Asia/Tehran
ConnectionStrings__DefaultConnection=
JwtSettings__SecretKey=
JwtSettings__Issuer=
JwtSettings__Audience=
JwtSettings__ExpiryMinutes=480
WebPush__VapidPublicKey=
WebPush__VapidPrivateKey=
WebPush__VapidSubject=
Cors__AllowedOrigins__0=https://drsaeedmoghadam.com
Cors__AllowedOrigins__1=https://www.drsaeedmoghadam.com
```

Do not copy production secrets into the image, manifest, or Git repository. Keep the SQL
Server outside the application container and restrict database network access to the backend.

## Routing and health

- Route `api.drsaeedmoghadam.com` to container port `8080`.
- Terminate TLS at the ArvanCloud route/CDN and redirect HTTP to HTTPS.
- Configure liveness on `GET /healthz` and readiness on `GET /readyz`.
- Verify WebSocket connectivity on `/hubs/reservations` after routing through the CDN.

The service applies EF Core migrations during startup. Deploy one backend replica at a time and
take a database backup before the first production deployment of a new version.

## Branch and environment mapping

| Branch | Runtime environment | Intended use |
| --- | --- | --- |
| `develop` | `Development` | Integration only; never connect it to production SQL |
| `stage` | `Stage` | Pre-production validation with separate secrets and database |
| `production` | `Production` | Live traffic and production secrets |

Promote the same reviewed commit from `develop` to `stage`, then from `stage` to
`production`. Do not rebuild from a different source commit during promotion.
