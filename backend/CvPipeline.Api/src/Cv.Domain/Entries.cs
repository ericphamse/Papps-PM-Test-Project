namespace CvPipeline.Api.Cv.Domain;

public record Referee(string Name, string Position, string Organisation, string Phone);
public record CareerEntry(string Title, string Organisation, string StartYear, string EndYear);
public record DatedEntry(string Description, string Year);
public record QualificationEntry(string Qualification, string? Institution, string Year);
public record Competency(string Text, string[] Criteria);
public record Highlight(string Heading, string Bullet, string[] Criteria);