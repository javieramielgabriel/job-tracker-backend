---
name: efcore-database-architect
description: >-
  Use this skill when working with Entity Framework Core (EF Core), DbContext
  configuration, Code First or Database First workflows, Fluent API entity
  configurations, migrations, seeding, LINQ query optimization, AsNoTracking,
  projections, eager/explicit/lazy loading, preventing N+1 queries, query
  splitting, transactions, concurrency handling, and compiled queries.
---

# EF Core Migration & Database Architect (`efcore-database-architect`)

## Overview
Provides comprehensive guidelines for designing, migrating, querying, and optimizing **Entity Framework Core** persistence layers in the **Job Tracker Backend** across **PostgreSQL (`Npgsql.EntityFrameworkCore.PostgreSQL`)** and **Oracle (`Oracle.EntityFrameworkCore`)** database providers.

## Activation Instructions
Activate this skill whenever you are:
- Configuring `DbContext`, entity mappings (`IEntityTypeConfiguration<T>`), relationships, value conversions, or data seeding.
- Creating, reviewing, scripting, applying, or rolling back EF Core migrations.
- Writing or optimizing EF Core LINQ queries, diagnosing N+1 issues, configuring split queries, handling transactions, or implementing optimistic concurrency.

## Specific Responsibilities
1. **Inspect Before Migrating**: Always check the active database provider, existing schema, and `__EFMigrationsHistory` before generating or applying migrations.
2. **Prevent Destructive Schema Changes**: Never automatically drop or recreate a database (`EnsureDeleted` / `dotnet ef database drop`). Carefully audit generated `Up` and `Down` migration methods and generated SQL scripts for data-loss operations (`DropTable`, `DropColumn`).
3. **Write High-Performance Queries**: Default to `AsNoTracking()` for read-only queries, project directly to DTOs with `.Select(...)`, eliminate N+1 queries, and use `AsSplitQuery()` when loading multiple child collections.
4. **Ensure Concurrency & Transactional Integrity**: Configure concurrency tokens (`xmin` on PostgreSQL or `RowVersion`/`ORA_ROWSCN` equivalents on Oracle) and explicit transactions where multi-step invariants require atomicity.

---

## Technology-Specific Implementation Guidance

### 1. DbContext Configuration & Provider Registration
```csharp
using JobTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Infrastructure.Persistence;

public sealed class JobTrackerDbContext(DbContextOptions<JobTrackerDbContext> options)
    : DbContext(options)
{
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<JobApplication> JobApplications => Set<JobApplication>();
    public DbSet<InterviewRound> InterviewRounds => Set<InterviewRound>();
    public DbSet<OfferDetails> Offers => Set<OfferDetails>();
    public DbSet<SkillTag> SkillTags => Set<SkillTag>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(JobTrackerDbContext).Assembly);
    }
}
```

### 2. Query Optimization: `AsNoTracking`, Projection, Split Queries & Avoiding N+1
- **Read-Only Queries**: Always use `.AsNoTracking()` when entities are not modified in the current unit of work.
- **Projection (`Select`)**: Project directly into DTOs so EF Core generates `SELECT` statements containing only the required columns rather than pulling full entity rows and blobs.
- **Avoiding N+1 Queries**: Never iterate over a query result and execute another query inside the loop. Use `.Select(...)` projection or `.Include(...)`.
- **Avoiding Cartesian Explosion**: When eager-loading multiple 1:N collections (`Include(a => a.Interviews).Include(a => a.SkillTags)`), apply `.AsSplitQuery()` so EF Core executes separate SQL queries instead of a multiplicative cross-join.

```csharp
public async Task<JobApplicationDetailDto?> GetApplicationDetailAsync(
    Guid id,
    CancellationToken ct = default)
{
    // Projection query: no tracking overhead, selects only needed columns
    return await dbContext.JobApplications
        .AsNoTracking()
        .AsSplitQuery()
        .Where(a => a.Id == id)
        .Select(JobApplicationMappings.ToDetailDtoProjection)
        .FirstOrDefaultAsync(ct);
}
```

### 3. Loading Strategies Comparison
- **Projection (`.Select(...)`)**: **Preferred** for API read endpoints. Fetches only requested columns.
- **Eager Loading (`.Include(...)` / `.ThenInclude(...)`)**: Best when fetching an aggregate root with child entities for domain mutation.
- **Explicit Loading (`dbContext.Entry(entity).Collection(x => x.Interviews).LoadAsync(ct)`)**: Useful when conditional logic determines whether related data is needed after loading the parent.
- **Lazy Loading**: **Avoid in Web APIs**—it triggers hidden synchronous or N+1 database round-trips during serialization.

### 4. Transactions & Optimistic Concurrency
```csharp
public async Task ArchiveCompanyAndApplicationsAsync(Guid companyId, CancellationToken ct)
{
    var strategy = dbContext.Database.CreateExecutionStrategy();

    await strategy.ExecuteAsync(async () =>
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(ct);

        await dbContext.JobApplications
            .Where(a => a.CompanyId == companyId && a.Status != ApplicationStatus.Accepted)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(a => a.Status, ApplicationStatus.Withdrawn)
                .SetProperty(a => a.UpdatedAtUtc, DateTimeOffset.UtcNow), ct);

        await transaction.CommitAsync(ct);
    });
}
```

### 5. Compiled Queries for Hot Paths
Use `EF.CompileAsyncQuery` for frequently executed lookups:
```csharp
private static readonly Func<JobTrackerDbContext, Guid, CancellationToken, Task<JobApplication?>>
    GetByIdCompiled = EF.CompileAsyncQuery(
        (JobTrackerDbContext db, Guid id, CancellationToken ct) =>
            db.JobApplications
                .Include(a => a.Company)
                .FirstOrDefault(a => a.Id == id));
```

See [Migrations and Query Optimization Reference](./references/migrations-and-query-optimization.md) for safe migration workflows, idempotent SQL generation, seeding, and concurrency handling.

---

## CLI Commands
```powershell
# List all migrations and check which are applied vs. pending
dotnet ef migrations list --project src/JobTracker.Infrastructure --startup-project src/JobTracker.Api

# Add a new migration
dotnet ef migrations add AddOfferDetailsTable --project src/JobTracker.Infrastructure --startup-project src/JobTracker.Api --output-dir Persistence/Migrations

# Generate an idempotent SQL script for review BEFORE applying to any database
dotnet ef migrations script --idempotent --project src/JobTracker.Infrastructure --startup-project src/JobTracker.Api --output ./artifacts/migration.sql

# Rollback to a specific previous migration
dotnet ef database update PreviousMigrationName --project src/JobTracker.Infrastructure --startup-project src/JobTracker.Api

# Remove the last unapplied migration
dotnet ef migrations remove --project src/JobTracker.Infrastructure --startup-project src/JobTracker.Api
```

---

## Common Mistakes to Avoid
1. **Materializing Too Early (`ToListAsync()` before `Where`/`Select`)**: Filtering in memory after calling `ToListAsync()` pulls the entire table over the network.
2. **Applying Unreviewed Migrations**: Running `dotnet ef database update` without inspecting the generated C# migration and SQL script can accidentally drop and recreate renamed columns, destroying data.
3. **Enabling Lazy Loading Proxies**: Causes N+1 queries and `ObjectDisposedException` during JSON serialization.
4. **Cartesian Explosion on Multi-Collection Includes**: Using multiple `.Include()` calls on 1:N collections without `.AsSplitQuery()`.

---

## Verification Checklist
- [ ] Read-only queries use `.AsNoTracking()` or project directly into DTOs via `.Select(...)`.
- [ ] Multi-collection `Include` queries use `.AsSplitQuery()` to avoid Cartesian explosion.
- [ ] Every generated migration has been inspected for unintended `DropColumn` or `DropTable` calls.
- [ ] Idempotent SQL scripts (`dotnet ef migrations script --idempotent`) have been reviewed against the target provider (PostgreSQL or Oracle).
- [ ] Bulk updates/deletes use `ExecuteUpdateAsync` / `ExecuteDeleteAsync` where appropriate.

---

## Official Documentation References
- [Entity Framework Core Documentation](https://learn.microsoft.com/en-us/ef/core/)
- [EF Core Efficient Querying](https://learn.microsoft.com/en-us/ef/core/performance/efficient-querying)
- [EF Core Migrations Overview](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/)
- [Npgsql EF Core Provider Documentation](https://www.npgsql.org/efcore/)
- [Oracle EF Core Provider Documentation](https://docs.oracle.com/en/database/oracle/oracle-data-access-components/)
