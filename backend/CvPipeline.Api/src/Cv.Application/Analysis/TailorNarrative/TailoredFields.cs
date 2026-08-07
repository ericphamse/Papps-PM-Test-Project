// src/Cv.Application/Analysis/TailorNarrative/TailoredFields.cs
namespace CvPipeline.Api.Cv.Application.Analysis.TailorNarrative;

public record TailoredCompetency(string Text, string[] Criteria);
public record TailoredHighlight(string Heading, string Bullet, string[] Criteria);

public record TailoredFields(
    string ProfessionalProfile,
    string RoleSuitability,
    string YearsOfExperience,
    List<TailoredCompetency> CoreCompetencies,
    List<TailoredHighlight> CareerHighlights
);