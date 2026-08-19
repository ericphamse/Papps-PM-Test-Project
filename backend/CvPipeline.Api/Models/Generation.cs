using System.Text.Json;

namespace CvPipeline.Api.Models;

public class Generation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AnalysisId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string OutputFilename { get; set; } = string.Empty;
    public JsonDocument Document { get; set; } = default!;
    public string FullName { get; set; } = string.Empty;
    public string RoleTitle { get; set; } = string.Empty;

    public Analysis Analysis { get; set; } = null!;
    public ICollection<GenerationField> GenerationFields { get; set; } = new List<GenerationField>();
}