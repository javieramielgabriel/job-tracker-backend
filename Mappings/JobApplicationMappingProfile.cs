using AutoMapper;
using JobTracker.Api.Models;

namespace JobTracker.Api.Mappings;

/// <summary>
/// AutoMapper profile for mapping between <see cref="JobApplication"/> entities and DTOs.
/// </summary>
public sealed class JobApplicationMappingProfile : Profile
{
    public JobApplicationMappingProfile()
    {
        // Entity -> Response DTO (orders nested interview phases deterministically by ScheduledAtUtc then Id)
        CreateMap<JobApplication, JobApplicationReadDto>()
            .ForCtorParam(
                nameof(JobApplicationReadDto.InterviewPhases),
                opt => opt.MapFrom(src => src.InterviewPhases
                    .OrderBy(phase => phase.ScheduledAtUtc)
                    .ThenBy(phase => phase.Id)
                    .ToList()));

        // Create Request DTO -> Entity
        CreateMap<JobApplicationCreateDto, JobApplication>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CompanyName, opt => opt.MapFrom(src => src.CompanyName.Trim()))
            .ForMember(dest => dest.JobTitle, opt => opt.MapFrom(src => src.JobTitle.Trim()))
            .ForMember(dest => dest.Location, opt => opt.MapFrom(src => src.Location != null ? src.Location.Trim() : null))
            .ForMember(dest => dest.CreatedAtUtc, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAtUtc, opt => opt.Ignore())
            .ForMember(dest => dest.Version, opt => opt.Ignore());

        // Update Request DTO -> Entity (scalar properties; nested InterviewPhases reconciliation is handled in the service layer)
        CreateMap<JobApplicationUpdateDto, JobApplication>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CompanyName, opt => opt.MapFrom(src => src.CompanyName.Trim()))
            .ForMember(dest => dest.JobTitle, opt => opt.MapFrom(src => src.JobTitle.Trim()))
            .ForMember(dest => dest.Location, opt => opt.MapFrom(src => src.Location != null ? src.Location.Trim() : null))
            .ForMember(dest => dest.CreatedAtUtc, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAtUtc, opt => opt.Ignore())
            .ForMember(dest => dest.Version, opt => opt.Ignore())
            .ForMember(dest => dest.InterviewPhases, opt => opt.Ignore());
    }
}
