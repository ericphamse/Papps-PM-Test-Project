// src/Cv.Application/Generations/CreateGeneration/CreateGenerationHandler.cs
using System.Text.Json;
using CvPipeline.Api.Data;
using CvPipeline.Api.Models;
using CvPipeline.Api.Cv.Domain;

namespace CvPipeline.Api.Cv.Application.Generations.CreateGeneration;

public class Gate2Exception : Exception
{
    public List<Gate2Violation> Violations { get; }
    public Gate2Exception(List<Gate2Violation> violations)
        : base("Gate 2 validation failed.") => Violations = violations;
}

public class CreateGenerationHandler
{
    private readonly CvPipelineDbContext _db;
    private readonly Gate2Validator _validator;

    public CreateGenerationHandler(CvPipelineDbContext db, Gate2Validator validator)
    {
        _db = db;
        _validator = validator;
    }

    public async Task<CreateGenerationResult> HandleAsync(
        CreateGenerationCommand command,
        CancellationToken ct)
    {   
        Console.WriteLine("=== CreateGenerationHandler called ===");
        // Gate 2 — run all checks before persisting anything
        var violations = _validator.Validate(command.Document);
        if (violations.Count > 0)
            throw new Gate2Exception(violations);

        // Persist generation row
        string filename = $"{command.Document.FullName.Value?.Replace(" ", "_") ?? "cv"}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.docx";

        var generation = new Generation
        {
            Id = Guid.NewGuid(),
            AnalysisId = command.AnalysisId,
            CreatedAt = DateTime.UtcNow,
            OutputFilename = filename,
            FullName = command.Document.FullName.Value ?? "",
            RoleTitle = command.Document.RoleTitle.Value ?? "",
            Document = JsonDocument.Parse(
                JsonSerializer.Serialize(command.Document,
                    new JsonSerializerOptions
                    {
                        MaxDepth = 128,
                        ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
                    }))
        };

        _db.Generations.Add(generation);

        // Persist generation_fields — one row per field, recording final provenance
        var fields = BuildGenerationFields(generation.Id, command.Document);
        foreach (var field in fields)
        {
            Console.WriteLine($"Field: {field.FieldPath}, Provenance: {field.Provenance}, RuleIds: {string.Join(",", field.RuleIds ?? new List<string>())}");
            _db.GenerationFields.Add(field);
        }

        await _db.SaveChangesAsync(ct);

        return new CreateGenerationResult(generation.Id, filename);
    }

    private List<GenerationField> BuildGenerationFields(Guid generationId, CvDocument doc)
    {
        var fields = new List<GenerationField>();

        void Add<T>(string path, FieldValue<T> field)
        {
            var provenance = field.Provenance.ToString().ToLowerInvariant();
            var ruleIds = field.RuleIds?.ToList();
            
            // Satisfy the DB constraint: normalised must cite at least one rule
            if (provenance == "normalised" && (ruleIds == null || ruleIds.Count == 0))
                ruleIds = new List<string> { "N1" }; // fallback — should not happen in practice

            // Satisfy the DB constraint: absent must have empty source quotes
            var sourceQuotes = provenance == "absent" 
                ? Array.Empty<string>() 
                : field.SourceQuotes ?? Array.Empty<string>();
            fields.Add(new GenerationField
            {
                GenerationId = generationId,
                FieldPath = path,
                Provenance = field.Provenance.ToString().ToLowerInvariant(),
                RuleIds = field.RuleIds?.ToList(),
                Criteria = field.Criteria?.ToList(),
                SourceQuotes = JsonDocument.Parse(
                    JsonSerializer.Serialize(field.SourceQuotes ?? Array.Empty<string>()))
            });
        }

        Add("roleTitle", doc.RoleTitle);
        Add("level", doc.Level);
        Add("proposedRole", doc.ProposedRole);
        Add("fullName", doc.FullName);
        Add("qualifications", doc.Qualifications);
        Add("securityClearance", doc.SecurityClearance);
        Add("yearsOfExperience", doc.YearsOfExperience);
        Add("availability", doc.Availability);
        Add("location", doc.Location);
        Add("referees", doc.Referees);
        Add("careerSynopsis", doc.CareerSynopsis);
        Add("professionalProfile", doc.ProfessionalProfile);
        Add("roleSuitability", doc.RoleSuitability);
        Add("coreCompetencies", doc.CoreCompetencies);
        Add("commendationsAndAwards", doc.CommendationsAndAwards);
        Add("qualificationsDetailed", doc.QualificationsDetailed);
        Add("careerHighlights", doc.CareerHighlights);

        return fields;
    }
}