using CareerSEA.Contracts.DTOs;

namespace CareerSEA.Services.Interfaces
{
    public interface ISkillGapService
    {
        Task<SkillGapEnvelopeDTO> GenerateSkillGapAsync(string bestJob, List<string> userSkills);
    }
}
