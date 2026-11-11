using JobTracker.Api.Common.Constants;
using JobTracker.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobTracker.Api.Data.Configurations;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="JobApplication"/> entity.
/// </summary>
public sealed class JobApplicationConfiguration : IEntityTypeConfiguration<JobApplication>
{
    public void Configure(EntityTypeBuilder<JobApplication> entity)
    {
        entity.ToTable("JobApplications");
        entity.HasKey(application => application.Id);

        entity.Property(application => application.CompanyName)
            .HasMaxLength(ApiConstants.Validation.MaxCompanyNameLength)
            .IsRequired();

        entity.Property(application => application.JobTitle)
            .HasMaxLength(ApiConstants.Validation.MaxJobTitleLength)
            .IsRequired();

        entity.Property(application => application.Location)
            .HasMaxLength(ApiConstants.Validation.MaxLocationLength);

        entity.Property(application => application.JobUrl)
            .HasMaxLength(ApiConstants.Validation.MaxJobUrlLength);

        entity.Property(application => application.Status)
            .HasConversion<string>()
            .HasMaxLength(ApiConstants.Validation.MaxStatusLength)
            .IsRequired();

        entity.Property(application => application.Notes)
            .HasMaxLength(ApiConstants.Validation.MaxApplicationNotesLength);

        entity.Property(application => application.CreatedAtUtc)
            .IsRequired();

        entity.Property(application => application.UpdatedAtUtc)
            .IsRequired();

        entity.Property(application => application.Version)
            .IsRowVersion();

        entity.HasIndex(application => new { application.Status, application.CreatedAtUtc })
            .HasDatabaseName("IX_JobApplications_Status_CreatedAtUtc");

        entity.HasMany(application => application.InterviewPhases)
            .WithOne(phase => phase.JobApplication)
            .HasForeignKey(phase => phase.JobApplicationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
