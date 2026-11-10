# Entity Relationships, Fluent API & Mapping Patterns Reference

## 1. Complete Fluent API Configuration for 1:1, 1:N, and M:N
Prefer `IEntityTypeConfiguration<T>` over cluttering domain entities with database-specific DataAnnotations:

```csharp
using JobTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobTracker.Infrastructure.Persistence.Configurations;

public sealed class JobApplicationConfiguration : IEntityTypeConfiguration<JobApplication>
{
    public void Configure(EntityTypeBuilder<JobApplication> builder)
    {
        builder.ToTable("job_applications");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.JobTitle)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.WorkMode)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.SalaryMin)
            .HasPrecision(12, 2);

        builder.Property(x => x.SalaryMax)
            .HasPrecision(12, 2);

        builder.Property(x => x.JobPostingUrl)
            .HasMaxLength(500);

        builder.Property(x => x.Notes)
            .HasMaxLength(4000);

        // 1:N Relationship: Company -> JobApplications
        builder.HasOne(x => x.Company)
            .WithMany(c => c.JobApplications)
            .HasForeignKey(x => x.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        // 1:1 Relationship: JobApplication -> OfferDetails
        builder.HasOne(x => x.Offer)
            .WithOne(o => o.JobApplication)
            .HasForeignKey<OfferDetails>(o => o.JobApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        // 1:N Relationship: JobApplication -> InterviewRounds
        builder.HasMany(x => x.Interviews)
            .WithOne(i => i.JobApplication)
            .HasForeignKey(i => i.JobApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        // M:N Relationship: JobApplication <-> SkillTag with explicit join table name
        builder.HasMany(x => x.SkillTags)
            .WithMany(s => s.JobApplications)
            .UsingEntity<Dictionary<string, object>>(
                "job_application_skills",
                right => right.HasOne<SkillTag>().WithMany().HasForeignKey("skill_tag_id").OnDelete(DeleteBehavior.Cascade),
                left => left.HasOne<JobApplication>().WithMany().HasForeignKey("job_application_id").OnDelete(DeleteBehavior.Cascade),
                join =>
                {
                    join.HasKey("job_application_id", "skill_tag_id");
                    join.ToTable("job_application_skills");
                });
    }
}
```

---

## 2. AutoMapper Profile Pattern (When Used by the Project)
When AutoMapper is adopted in a project, always configure explicit projections and verify configuration in unit tests:

```csharp
using AutoMapper;
using JobTracker.Application.Contracts.Applications;
using JobTracker.Domain.Entities;

namespace JobTracker.Application.Mappings;

public sealed class JobApplicationMappingProfile : Profile
{
    public JobApplicationMappingProfile()
    {
        CreateMap<Company, CompanyBriefDto>();
        CreateMap<OfferDetails, OfferDetailsDto>();
        CreateMap<InterviewRound, InterviewRoundDto>();
        CreateMap<SkillTag, SkillTagDto>();

        CreateMap<JobApplication, JobApplicationDetailDto>()
            .ForCtorParam(nameof(JobApplicationDetailDto.Skills),
                opt => opt.MapFrom(src => src.SkillTags.OrderBy(s => s.Name)));
    }
}
```
