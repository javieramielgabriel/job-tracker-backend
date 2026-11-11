using JobTracker.Api.Common.Constants;
using JobTracker.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Api.Data;

/// <summary>
/// Entity Framework Core database context for the Job Tracker application.
/// </summary>
public sealed class JobTrackerDbContext(DbContextOptions<JobTrackerDbContext> options)
    : DbContext(options)
{
    public DbSet<JobApplication> JobApplications => Set<JobApplication>();
    public DbSet<InterviewPhase> InterviewPhases => Set<InterviewPhase>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(ApiConstants.DatabaseSchema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(JobTrackerDbContext).Assembly);
    }
}
