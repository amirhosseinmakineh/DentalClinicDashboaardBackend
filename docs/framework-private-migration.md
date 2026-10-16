# Framework private package migration

This branch is based on `code_lftom`. **Do not remove Framework source or change the project reference yet.** The current Dockerfile restores directly from source and the customer-controlled Liara deployment must continue to build.

## Current state

- `DentalDashboard.Framwork` is packable as `DentalDashboard.Framework` version `1.0.0`.
- `.github/workflows/framework-package.yml` restores, builds, packs, and checks that the package contains the DLL but no C# source or PDB files.
- The workflow uploads a temporary `.nupkg` artifact. An Actions artifact is **not** a NuGet feed.

## Remaining migration steps

1. Create a separate private repository under `amirhosseinmakineh` for the Framework source, and grant access only to maintainers.
2. Move Framework source into that private repository and publish the NuGet package to an authenticated private feed.
3. Give the backend build process **read-only** access to the feed. Do not commit tokens or pass them through Docker build arguments or environment variables; use a supported build secret or a trusted CI image build.
4. Replace the Framework `ProjectReference` in `DentalDashboard.Domain` with a pinned `PackageReference`; update the solution and Dockerfile together.
5. Build and test the full backend and Docker image without the Framework source in the backend checkout.
6. Only then remove the Framework source from the deployment repository and deploy during an approved release window.

## Verification

```bash
dotnet restore DentalDashboard.slnx
dotnet build DentalDashboard.slnx --configuration Release --no-restore
dotnet pack DentalDashboard.Framwork/DentalDashboard.Framwork.csproj --configuration Release --no-build --output ./artifacts/framework
```

A DLL cannot be made immutable against a customer who controls the host, and prior copies of the source remain outside this migration's control. This protects future source distribution rather than guaranteeing non-bypassable licensing.
