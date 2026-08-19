using System.Text.Json;

namespace CvPipeline.Api.Models;

public class GenerationField
{
    public Guid GenerationId { get; set; }
    public string FieldPath { get; set; } = string.Empty;
    public string Provenance { get; set; } = string.Empty;
    public List<string>? RuleIds { get; set; }
    public List<string>? Criteria { get; set; }
    public JsonDocument SourceQuotes { get; set; } = JsonDocument.Parse("[]");

    public Generation Generation { get; set; } = null!;
}