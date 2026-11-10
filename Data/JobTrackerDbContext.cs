using JobTracker.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Api.Data;

public sealed class JobTrackerDbContext(DbContextOptions<JobTrackerDbContext> options)
    : DbContext(options)
{
    public DbSet<JobApplication> JobApplications => Set<JobApplication>();
    public DbSet<InterviewPhase> InterviewPhases => Set<InterviewPhase>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("JobTracker");

        modelBuilder.Entity<JobApplication>(entity =>
        {
            entity.ToTable("JobApplications");
            entity.HasKey(application => application.Id);
            entity.Property(application => application.CompanyName)
                .HasMaxLength(200)
                .IsRequired();
            entity.Property(application => application.JobTitle)
                .HasMaxLength(200)
                .IsRequired();
            entity.Property(application => application.Location)
                .HasMaxLength(150);
            entity.Property(application => application.JobUrl)
                .HasMaxLength(2048);
            entity.Property(application => application.Status)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();
            entity.Property(application => application.Notes)
                .HasMaxLength(4000);
            entity.Property(application => application.CreatedAtUtc)
                .IsRequired();
            entity.Property(application => application.UpdatedAtUtc)
                .IsRequired();
            entity.HasMany(application => application.InterviewPhases)
                .WithOne(phase => phase.JobApplication)
                .HasForeignKey(phase => phase.JobApplicationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<InterviewPhase>(entity =>
        {
            entity.ToTable("InterviewPhases");
            entity.HasKey(phase => phase.Id);
            entity.Property(phase => phase.Name)
                .HasMaxLength(100)
                .IsRequired();
            entity.Property(phase => phase.Notes)
                .HasMaxLength(2000);
        });
    }
}
