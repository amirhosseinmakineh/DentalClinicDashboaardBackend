# DentalDashboard Framework

DentalDashboard Framework contains the shared CQRS contracts, result types,
repository abstractions, and base domain types used by DentalDashboard.

The package is designed to be consumed as a compiled `DentalDashboard.Framework`
NuGet package. It has no reference to the DentalDashboard backend projects.

## Compatibility

- Target framework: `net10.0`
- Package ID: `DentalDashboard.Framework`
- Package contents exclude C# source, PDB files, and symbol packages.

## Future private repository

The intended source repository is
`amirhosseinmakineh/DentalDashboard-Framework`. Until that repository is created,
this source remains staged in the backend repository and is not private.

## Licensing integration

The package exposes licensing contracts only. Applications decide when and how to
validate a signed license; this package does not contact a license server or enforce
license state by itself.
