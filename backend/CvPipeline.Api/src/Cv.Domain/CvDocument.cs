namespace CvPipeline.Api.Cv.Domain;

public enum Provenance { Verbatim, Normalised, Derived, Generated, Edited, Absent }

public record FieldValue<T>(
    T? Value,
    Provenance Provenance,
    string[] SourceQuotes,
    string[]? RuleIds = null,
    string[]? Criteria = null
);

public record CvDocument(
    int SchemaVersion,
    FieldValue<string> RoleTitle,
    FieldValue<string> Level,
    FieldValue<string> ProposedRole,
    FieldValue<string> FullName,
    FieldValue<List<string>> Qualifications,
    FieldValue<string> SecurityClearance,
    FieldValue<string> YearsOfExperience,
    FieldValue<string> Availability,
    FieldValue<string> Location,
    FieldValue<List<Referee>> Referees,
    FieldValue<List<CareerEntry>> CareerSynopsis,
    FieldValue<string> ProfessionalProfile,
    FieldValue<string> RoleSuitability,
    FieldValue<List<Competency>> CoreCompetencies,
    FieldValue<List<DatedEntry>> CommendationsAndAwards,
    FieldValue<List<QualificationEntry>> QualificationsDetailed,
    FieldValue<List<Highlight>> CareerHighlights
);