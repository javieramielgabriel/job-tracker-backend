---
name: dotnet-package-management
description: >-
  Use this skill when inspecting, adding, removing, restoring, updating, or
  troubleshooting NuGet packages and dependencies in .NET projects. Covers
  dotnet add/remove/restore/list package, Central Package Management
  (Directory.Packages.props), version conflict resolution, transitive
  dependencies, TargetFramework compatibility, vulnerability auditing, and
  package migrations.
---

# NuGet Package Management (`dotnet-package-management`)

## Overview
Provides structured workflows and safety protocols for managing NuGet dependencies, Central Package Management (`Directory.Packages.props`), version conflict resolution, and security vulnerability auditing in the **Job Tracker Backend** on **.NET 10 (`net10.0`)**.

## Activation Instructions
Activate this skill whenever you are:
- Inspecting existing NuGet dependencies, transitive packages, outdated versions, or security advisories (`dotnet list package`).
- Resolving NuGet version conflicts (`NU1107`, `NU1605`, `NU1202`), diamond dependency mismatches, or framework compatibility issues.
- Setting up or maintaining Central Package Management (`Directory.Packages.props`).
- Planning safe package upgrades or migrations across major versions.

## Specific Responsibilities
1. **Follow the 7-Step Conflict Resolution Protocol**:
   1. Inspect `.csproj` and `Directory.Packages.props` files.
   2. Inspect currently resolved package versions (`dotnet list package --include-transitive`).
   3. Identify the exact conflicting direct and transitive dependencies.
   4. Check `<TargetFramework>` (`net10.0`) compatibility for all candidate versions.
   5. Recommend aligned, stable versions (keep `Microsoft.EntityFrameworkCore.*` and `Microsoft.Extensions.*` families on matching major.minor.patch versions).
   6. Document any breaking changes between versions.
   7. Verify `dotnet restore`, `dotnet build`, and `dotnet test`.
2. **Never Blindly Upgrade/Downgrade or Use Prerelease Packages**: Always prefer stable releases unless a feature specifically requires a preview package approved by the user.
3. **Audit for Security Vulnerabilities**: Regularly run `dotnet list package --vulnerable --include-transitive`.

---

## Technology-Specific Implementation Guidance

### 1. Central Package Management (`Directory.Packages.props`)
In multi-project solutions (`JobTracker.Domain`, `JobTracker.Application`, `JobTracker.Infrastructure`, `JobTracker.Api`, `JobTracker.UnitTests`), centralize all package versions in a single root `Directory.Packages.props` file so every project uses identical versions:

```xml
<!-- Root: Directory.Packages.props -->
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
    <CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>
  </PropertyGroup>

  <ItemGroup Label="ASP.NET Core &amp; API">
    <PackageVersion Include="Microsoft.AspNetCore.OpenApi" Version="10.0.0" />
    <PackageVersion Include="Asp.Versioning.Mvc" Version="8.1.0" />
    <PackageVersion Include="Asp.Versioning.Mvc.ApiExplorer" Version="8.1.0" />
  </ItemGroup>

  <ItemGroup Label="Entity Framework Core &amp; Database Providers">
    <PackageVersion Include="Microsoft.EntityFrameworkCore" Version="10.0.0" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.0" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.Relational" Version="10.0.0" />
    <PackageVersion Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.0" />
    <PackageVersion Include="Oracle.EntityFrameworkCore" Version="10.23.60" />
  </ItemGroup>

  <ItemGroup Label="CLI">
    <PackageVersion Include="System.CommandLine" Version="2.0.0-beta4.22272.1" />
  </ItemGroup>

  <ItemGroup Label="Testing &amp; Coverage">
    <PackageVersion Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
    <PackageVersion Include="xunit" Version="2.9.2" />
    <PackageVersion Include="xunit.runner.visualstudio" Version="2.8.2" />
    <PackageVersion Include="Moq" Version="4.20.72" />
    <PackageVersion Include="NSubstitute" Version="5.3.0" />
    <PackageVersion Include="coverlet.collector" Version="6.0.2" />
    <PackageVersion Include="Microsoft.AspNetCore.Mvc.Testing" Version="10.0.0" />
  </ItemGroup>
</Project>
```

Individual `.csproj` files then reference packages **without** the `Version` attribute:
```xml
<ItemGroup>
  <PackageReference Include="Microsoft.EntityFrameworkCore" />
  <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" />
</ItemGroup>
```

### 2. Resolving Common NuGet Errors
- **`NU1605` (Detected package downgrade)**: A transitive dependency requests a higher version than a direct `PackageReference`. Align the direct reference in `Directory.Packages.props` to match or exceed the transitive version.
- **`NU1107` (Version conflict detected)**: Two packages require incompatible versions of a shared dependency (commonly `Microsoft.EntityFrameworkCore.Relational` between `Design` and a database provider like `Npgsql` or `Oracle`). Align the EF Core major/minor version to the highest version supported by the database provider.
- **`NU1901`–`NU1904` (NuGet Audit security warnings)**: A direct or transitive package has a known CVE advisory. Use `CentralPackageTransitivePinningEnabled` to pin a patched transitive package version if the parent package hasn't updated yet.

See [Central Package Management and Auditing Reference](./references/central-package-management-and-auditing.md) for `nuget.config`, NuGet Audit settings, and dependency graph inspection.

---

## CLI Commands
```powershell
# List all direct and transitive packages in the solution
dotnet list package --include-transitive

# Check for known security vulnerabilities across direct and transitive dependencies
dotnet list package --vulnerable --include-transitive

# Check for outdated packages (excluding prerelease versions)
dotnet list package --outdated

# Check for deprecated packages
dotnet list package --deprecated --include-transitive

# Restore packages and verify lock/assets files
dotnet restore

# Add a stable package to a specific project (when approved by user)
dotnet add src/JobTracker.Infrastructure/JobTracker.Infrastructure.csproj package Npgsql.EntityFrameworkCore.PostgreSQL

# Remove an unused package from a project (when approved by user)
dotnet remove src/JobTracker.Infrastructure/JobTracker.Infrastructure.csproj package UnusedPackageName
```

---

## Common Mistakes to Avoid
1. **Mismatched EF Core Package Versions**: Mixing `Microsoft.EntityFrameworkCore.Design` `10.0.x` with an `8.0.x` or `9.0.x` database provider causes runtime `MissingMethodException` or `TypeLoadException`.
2. **Installing `Microsoft.EntityFrameworkCore` in the Domain Project**: Keep the Domain layer free of persistence and infrastructure NuGet packages.
3. **Blindly Upgrading Major Versions**: Major version bumps can introduce breaking API, behavior, or database mapping changes. Always review release notes first.
4. **Missing `<PrivateAssets>all</PrivateAssets>` on Build/Analyzer Packages**: Packages like `Microsoft.EntityFrameworkCore.Design`, `xunit.runner.visualstudio`, and `coverlet.collector` should include `<PrivateAssets>all</PrivateAssets>` so they do not flow transitively to consuming projects.

---

## Verification Checklist
- [ ] All projects in the solution use consistent package versions (preferably via `Directory.Packages.props`).
- [ ] `Microsoft.EntityFrameworkCore.*` packages share the exact same major/minor version compatible with the active database provider.
- [ ] `dotnet list package --vulnerable --include-transitive` reports zero known vulnerabilities.
- [ ] Build-only and test analyzer packages set `<PrivateAssets>all</PrivateAssets>`.
- [ ] `dotnet restore`, `dotnet build`, and `dotnet test` succeed without `NU*` warnings.

---

## Official Documentation References
- [NuGet Documentation](https://learn.microsoft.com/en-us/nuget/)
- [Central Package Management (CPM)](https://learn.microsoft.com/en-us/nuget/consume-packages/central-package-management)
- [Auditing Package Dependencies for Security Vulnerabilities](https://learn.microsoft.com/en-us/nuget/concepts/auditing-packages)
- [Package Dependency Resolution Rules](https://learn.microsoft.com/en-us/nuget/concepts/dependency-resolution)
