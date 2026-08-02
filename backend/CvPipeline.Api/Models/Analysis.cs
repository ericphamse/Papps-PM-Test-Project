using System.Text.Json;

namespace CvPipeline.Api.Models;

public class Analysis
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string CvText { get; set; } = string.Empty;
    public string JobRequirements { get; set; } = string.Empty;
    public string CvSourceFilename { get; set; } = string.Empty;
    public string Status { get; set; } = AnalysisStatus.Extracting;
    
    public JsonDocument? FailureDetail { get; set; }
    public JsonDocument? Document { get; set; }
    public JsonDocument Warnings { get; set; } = JsonDocument.Parse("[]");

    public ICollection<Generation> Generations { get; set; } = new List<Generation>();
}