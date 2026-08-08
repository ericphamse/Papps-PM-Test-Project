namespace CvPipeline.Api.Cv.Application.Analysis.ExtractCv;

// One field's evidence from stage 1 — a span, not a decided value
public record FieldQuotes(string[] SourceQuotes);

public record ReferQuotes(
    FieldQuotes Name,
    FieldQuotes Position,
    FieldQuotes Organisation,
    FieldQuotes Phone
);

public record CareerEntryQuotes(
    FieldQuotes Title,
    FieldQuotes Organisation,
    FieldQuotes StartYear,
    FieldQuotes EndYear
);

public record TechVote(
    string Technology,
    bool Keep,
    string? RfqQuote
);

public record DatedEntryQuotes(FieldQuotes Description, FieldQuotes Year);
public record QualificationEntryQuotes(FieldQuotes Qualification, FieldQuotes Institution, FieldQuotes Year);

//stage 1 response
public record SelectionResult(
    FieldQuotes FullName,
    FieldQuotes Qualifications,
    FieldQuotes SecurityClearance,
    FieldQuotes YearsOfExperience,
    FieldQuotes Availability,
    FieldQuotes Location,
    List<ReferQuotes> Referees,
    List<CareerEntryQuotes> CareerSynopsis,
    List<DatedEntryQuotes> CommendationsAndAwards,
    List<QualificationEntryQuotes> QualificationsDetailed,
    List<TechVote> TechVotes
);