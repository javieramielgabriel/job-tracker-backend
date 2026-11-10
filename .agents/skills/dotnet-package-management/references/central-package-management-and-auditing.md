# Central Package Management, NuGet Audit & Sources Reference

## 1. Configuring NuGet Audit in `Directory.Build.props`
Enable automatic security vulnerability auditing during `dotnet restore` across all projects:

```xml
<!-- Root: Directory.Build.props -->
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>

    <!-- NuGet Security Auditing -->
    <NuGetAudit>true</NuGetAudit>
    <NuGetAuditMode>all</NuGetAuditMode>
    <NuGetAuditLevel>low</NuGetAuditLevel>
  </PropertyGroup>
</Project>
```

---

## 2. Standard `nuget.config` with Package Source Mapping
```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
  </packageSources>
  <packageSourceMapping>
    <packageSource key="nuget.org">
      <package pattern="*" />
    </packageSource>
  </packageSourceMapping>
</configuration>
```

---

## 3. Pinning a Vulnerable Transitive Dependency
With `<CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>` in `Directory.Packages.props`, you can override a vulnerable transitive package without adding a direct `<PackageReference>` to every `.csproj`:
```xml
<ItemGroup Label="Transitive Security Pins">
  <PackageVersion Include="System.Text.Json" Version="10.0.0" />
</ItemGroup>
```
