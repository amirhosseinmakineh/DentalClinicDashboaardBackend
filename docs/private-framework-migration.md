# Private Framework migration plan

Status: **package build validated; source isolation not yet performed**.

## Verified current state

- Framework project: `DentalDashboard.Framwork/DentalDashboard.Framwork.csproj` (existing spelling retained).
- Target: .NET 10; package ID: `DentalDashboard.Framework`.
- `DentalDashboard.Domain` directly references the Framework project.
- The Dockerfile copies the Framework project and builds from repository source.
- `production` has a GitHub Actions workflow that may deploy to Liara on push. Do not merge this work until deployment is coordinated.

## Required external setup

1. Create a **private** repository owned by the maintainer for Framework source. Restrict access independently of the customer-facing backend repository.
2. Configure a private NuGet feed (for example, GitHub Packages) with scoped read-only credentials for build jobs. Publishing credentials must remain in the private framework repository.
3. Move the Framework source to that repository, preserving namespace and public API compatibility.
4. Publish a versioned `.nupkg` to the private feed, and validate that the backend can restore it using **only read access**.
5. For source-based Liara builds, use BuildKit secret mounts for NuGet credentials or a trusted CI build that produces a deployable image. **Never** copy credentials into the Docker build context, commit them to `NuGet.Config`, or use Docker `ARG`/`ENV` for secrets.
6. Change the Domain reference from `ProjectReference` to `PackageReference` after the feed and authenticated restore are verified. Update Dockerfile and solution references in the same change.
7. Remove the Framework source from the backend deploy repository only after the full solution and container build pass against the package. Check that no source files or debug symbols are bundled.
8. Deploy through an approved release window with a tested rollback plan.

## Local packaging

```bash
dotnet restore DentalDashboard.slnx
dotnet build DentalDashboard.slnx -c Release --no-restore
dotnet pack DentalDashboard.Framwork/DentalDashboard.Framwork.csproj -c Release --no-build -o ./artifacts/framework
```

The feature-branch workflow `.github/workflows/framework-package.yml` validates these steps and retains the package temporarily as a GitHub Actions artifact. **That artifact is not a private NuGet feed.**

## Security boundaries

A compiled .NET assembly can be inspected, patched, or replaced by someone who controls the runtime. This migration protects new source distribution; it cannot revoke previously delivered copies or guarantee enforcement of licensing. Do not use database disruption or hidden service shutdowns as license enforcement.
