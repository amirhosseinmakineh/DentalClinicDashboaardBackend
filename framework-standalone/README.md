# Standalone Framework staging area

This directory is a **non-destructive copy** of the original `DentalDashboard.Framwork` project, preserving its namespace, source files and project file.

It is staged in the existing public repository **only until a private repository is created**. It is not private, and it is not a security boundary.

When the private repository is available, transfer the contents of this directory into its repository root. Do not copy the backend solution or application secrets. Publish the resulting `DentalDashboard.Framework` NuGet package from trusted CI. Leave the original source in place until an approved, verified cutover.

Build from repository root:
```bash
dotnet restore framework-standalone/DentalDashboard.Framwork.csproj
dotnet build framework-standalone/DentalDashboard.Framwork.csproj -c Release
dotnet pack framework-standalone/DentalDashboard.Framwork.csproj -c Release -o artifacts/framework-standalone
```
