namespace CvPipeline.Api.Features.Analyses.CreateAnalysis;

public record ProvenanceValue<T>(
    T? Value,
    string Provenance,
    string? RuleId = null
);

public record PersonalDetailsDto(
    ProvenanceValue<string> FullName,
    ProvenanceValue<string> Location,
    ProvenanceValue<string> SecurityClearance,
    ProvenanceValue<string> Referees
);

public record WorkHistoryDto(
    ProvenanceValue<string> JobTitle,
    ProvenanceValue<string> Company,
    ProvenanceValue<string> Dates,
    List<string> BulletPoints
);

public record EducationDto(
    ProvenanceValue<string> Degree,
    ProvenanceValue<string> Institution
);