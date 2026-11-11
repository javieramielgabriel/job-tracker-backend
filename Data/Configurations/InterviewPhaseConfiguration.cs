using JobTracker.Api.Common.Constants;
using JobTracker.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobTracker.Api.Data.Configurations;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="InterviewPhase"/> entity.
/// </summary>
public sealed class InterviewPhaseConfiguration : IEntityTypeConfiguration<InterviewPhase>
{
    public void Configure(EntityTypeBuilder<InterviewPhase> entity)
    {
        entity.ToTable("InterviewPhases");
        entity.HasKey(phase => phase.Id);

        entity.Property(phase => phase.Name)
            .HasMaxLength(ApiConstants.Validation.MaxPhaseNameLength)
            .IsRequired();

        entity.Property(phase => phase.Notes)
            .HasMaxLength(ApiConstants.Validation.MaxPhaseNotesLength);

        entity.Property(phase => phase.Version)
            .IsRowVersion();

        entity.HasIndex(phase => new { phase.JobApplicationId, phase.ScheduledAtUtc })
            .HasDatabaseName("IX_InterviewPhases_JobApplicationId_ScheduledAtUtc");
    }
}
