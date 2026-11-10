# EF Core Migrations, Seeding & Concurrency Reference

## 1. Safe Migration Review Workflow
Before applying any migration:
1. **Check Provider & History**: Run `dotnet ef dbcontext info` and `dotnet ef migrations list`.
2. **Generate Migration**: `dotnet ef migrations add <Name>`.
3. **Inspect `Up` and `Down`**:
   - Look for `migrationBuilder.DropColumn` or `migrationBuilder.DropTable` (EF Core sometimes generates drop + add when a rename was intended—replace with `migrationBuilder.RenameColumn`).
   - Verify non-nullable columns added to populated tables have an explicit `defaultValue` or a two-step backfill SQL migration.
4. **Generate & Inspect SQL**:
   ```powershell
   dotnet ef migrations script FromMigration ToMigration --idempotent -o review.sql
   ```

---

## 2. Modern Data Seeding (.NET 9 / .NET 10 `UseAsyncSeeding`)
Prefer `UseSeeding` and `UseAsyncSeeding` in `DbContextOptionsBuilder` over `HasData` for reference and development data that doesn't belong hardcoded inside migration snapshots:
```csharp
builder.Services.AddDbContext<JobTrackerDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("JobTrackerDb"))
        .UseAsyncSeeding(async (context, _, ct) =>
        {
            if (!await context.SkillTags.AnyAsync(ct))
            {
                context.SkillTags.AddRange(
                    new SkillTag { Name = "C#" },
                    new SkillTag { Name = "ASP.NET Core" },
                    new SkillTag { Name = "Entity Framework Core" },
                    new SkillTag { Name = "PostgreSQL" },
                    new SkillTag { Name = "Oracle" });
                await context.SaveChangesAsync(ct);
            }
        });
});
```

---

## 3. Optimistic Concurrency Handling
Configure a concurrency token on entities subject to concurrent updates:
```csharp
// In Entity:
public uint Version { get; set; } // Or Guid ConcurrencyStamp { get; set; } = Guid.NewGuid();

// Provider-agnostic Concurrency Token in Fluent API:
builder.Property(x => x.ConcurrencyStamp)
    .IsConcurrencyToken();
```
Catch `DbUpdateConcurrencyException` in the service/repository layer and return a `409 Conflict` `ProblemDetails` response.
