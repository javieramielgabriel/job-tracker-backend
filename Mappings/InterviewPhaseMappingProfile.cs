using AutoMapper;
using JobTracker.Api.Models;

namespace JobTracker.Api.Mappings;

/// <summary>
/// AutoMapper profile for mapping between <see cref="InterviewPhase"/> entities and DTOs.
/// </summary>
public sealed class InterviewPhaseMappingProfile : Profile
{
    public InterviewPhaseMappingProfile()
    {
        // Entity -> Response DTO
        CreateMap<InterviewPhase, InterviewPhaseReadDto>();

        // Request DTO -> Entity (with string sanitization; persistence metadata is managed by the service/ORM)
        CreateMap<InterviewPhaseDto, InterviewPhase>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.JobApplicationId, opt => opt.Ignore())
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name.Trim()))
            .ForMember(dest => dest.Version, opt => opt.Ignore())
            .ForMember(dest => dest.JobApplication, opt => opt.Ignore());
    }
}
