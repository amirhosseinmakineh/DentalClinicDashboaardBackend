# GitHub Packages release guide

The workflow `.github/workflows/framework-publish.yml` is **manual only** (`workflow_dispatch`).
It publishes the Framework NuGet package to the GitHub Packages feed for `amirhosseinmakineh`.

## Before publishing

- Confirm the package version is new and not already in use.
- Confirm repository Actions policy permits publishing packages with `GITHUB_TOKEN`.
- **Security:** This workflow currently reads Framework source from the existing backend repository. It does not make that source private. Do not give the customer write access to the source/publish workflow.
- A private source repository with a separate release workflow remains the recommended final architecture.

## Publish

In GitHub, open Actions > Publish Framework to GitHub Packages > Run workflow. Select the feature branch and supply a new version (for example `1.0.1`). Verify the workflow result and package visibility/access in GitHub Packages.

## Consumption

Configure an authenticated NuGet source with `https://nuget.pkg.github.com/amirhosseinmakineh/index.json` and read-only `read:packages` permissions. Update `FrameworkPackageVersion` at build time or in the Domain project after confirming the package exists. Never put credentials into source control.

## Deployment safety

Publishing a package does not automatically deploy or change the active Dockerfile. Test a package restore and container build first. Keep the existing source-based build and rollback image until an approved cutover.
