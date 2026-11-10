# Coverlet Thresholds, ReportGenerator & Cobertura Analysis Reference

## 1. Clean & Generate Coverage Report One-Step Workflow (PowerShell)
```powershell
# Remove previous test results to avoid merging stale Cobertura XML files
if (Test-Path ./TestResults) { Remove-Item -Recurse -Force ./TestResults }

# Run tests with XPlat Code Coverage
dotnet test --collect:"XPlat Code Coverage" --results-directory ./TestResults

# Run ReportGenerator (if installed as a local or global dotnet tool)
dotnet tool run reportgenerator `
  -reports:"./TestResults/**/coverage.cobertura.xml" `
  -targetdir:"./TestResults/CoverageReport" `
  -reporttypes:"Html;TextSummary;MarkdownSummaryGithub;Badges"
```

---

## 2. Recommended Coverage Targets by Layer

| Layer | Line Coverage Target | Branch Coverage Target | Rationale |
| :--- | :--- | :--- | :--- |
| **Domain Layer** (Entities, Value Objects, Rules) | >= 90% | >= 85% | Core business invariants must be exhaustively verified. |
| **Application Layer** (Services, Validators, Mappers) | >= 85% | >= 80% | Orchestration, state transitions, and validation rules. |
| **API Layer** (Controllers, Middleware, Exception Handlers) | >= 75% | >= 70% | Verified via unit tests + `WebApplicationFactory` integration tests. |
| **Infrastructure Layer** (EF Migrations, Design-Time Factories) | Excluded / Integration Tested | N/A | Tested via integration tests; migrations excluded from unit coverage. |

---

## 3. Enforcing Coverage Thresholds with `coverlet.msbuild` (CI/CD)
When `coverlet.msbuild` is used in CI pipelines, thresholds can be enforced per build:
```powershell
dotnet test /p:CollectCoverage=true `
  /p:CoverletOutputFormat=cobertura `
  /p:Threshold=80 `
  /p:ThresholdType="line,branch" `
  /p:ThresholdStat=total
```
