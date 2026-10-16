# Private Framework deployment (staged, no deletions)

The normal `Dockerfile` remains unchanged. `Dockerfile.package` builds from a local `.nupkg`; `Dockerfile.private` restores from an authenticated NuGet feed using a Docker BuildKit secret.

## Prerequisites

1. Create a private GitHub repository owned by `amirhosseinmakineh` and move the Framework source there when ready. Existing backend source stays intact for now.
2. Publish `DentalDashboard.Framework` version `1.0.0` to GitHub Packages from the private repository.
3. Supply a read-only package credential to the trusted builder. Use `deploy/NuGet.Private.Config.example` as a template, save the populated file locally as `NuGet.Private.Config` (gitignored), and add a `packageSourceCredentials` entry for `github-private` with a username and read-only token. Never commit the populated config.
4. Ensure the builder can access the private package. A customer-owned source-build host cannot keep its read credentials secret from the host administrator; prefer trusted CI building and pushing the final container image.

## Trusted builder command

```bash
DOCKER_BUILDKIT=1 docker build \
  --secret id=nuget_config,src=NuGet.Private.Config \
  -f Dockerfile.private \
  -t dental-back:private .
```

The private Dockerfile uses a required secret mount only for restore. No token is passed through Docker `ARG`, Docker `ENV`, or a committed NuGet config.

## Validation before switching deployment

- Publish and restore the private package from a trusted CI environment.
- Run `dotnet build` in package mode without Framework source.
- Build `Dockerfile.private` without Framework source in the build context.
- Run smoke tests and confirm container health and the application startup.
- Confirm a rollback image exists.
- Only then change the actual deployment pipeline, with explicit approval.

**Important:** Compiled assemblies are not tamper-proof when customers control the server. Removing Framework source from future deployments does not revoke source already distributed or visible in Git history.
