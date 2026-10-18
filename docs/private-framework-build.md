# Backend builds with the private Framework package

The backend consumes the exact `DentalDashboard.Framework` version `1.0.0` from the private `amirhosseinmakineh/DentalDashboard-Framework` repository via GitHub Packages. The assembly and namespaces keep their existing `DentalDashboard.Framwork` spelling. Framework source is no longer in this branch or solution.

## Local build

Use an account with read access to the package. Make a local credentials file (ignored by Git and Docker):

```bash
cp NuGet.Config NuGet.Private.Config
dotnet nuget update source github-private --username YOUR_GITHUB_USERNAME --password "$NUGET_READ_TOKEN" --store-password-in-clear-text --configfile NuGet.Private.Config
dotnet restore DentalDashboard.slnx --configfile NuGet.Private.Config
dotnet build DentalDashboard.slnx --configuration Release --no-restore
```

The token must have package read permission. Never commit the populated configuration.

## Container build

```bash
docker build --secret id=nuget_config,src=NuGet.Private.Config -t dental-back:private .
```

BuildKit mounts the configuration only during restore. The publish step uses `--no-restore`; credentials are not copied into the image. Source-only deployment builders must supply this secret, or deployment must consume an image already built by trusted CI.

## CI

All backend build workflows use the shared `setup-private-framework` action and their read-only `GITHUB_TOKEN`. The private package must grant this backend repository Actions read access; the existing private-consumer preflight has verified that access. CI builds the complete solution and container without Framework source.

This branch does not implement license enforcement and does not change Production. Historical commits and older branches may still contain previously published Framework source.
