using CvPipeline.Api.Data;
using CvPipeline.Api.Models;

namespace CvPipeline.Api.Features.Analyses.CreateAnalysis;

public record CreateAnalysisRequest(
    string RawCvText,
    string? JobDescription = null
);

public record CreateAnalysisResponse(
    Guid AnalysisId,
    RawExtractedCv ExtractedData
);

public class CreateAnalysisHandler
{
    private readonly CvPipelineDbContext _db;
    private readonly GeminiExtractorService _aiExtractor;

    public CreateAnalysisHandler(
        CvPipelineDbContext db,
        GeminiExtractorService aiExtractor)
    {
        _db = db;
        _aiExtractor = aiExtractor;
    }

    public async Task<CreateAnalysisResponse> HandleAsync(
        CreateAnalysisRequest request, 
        CancellationToken ct = default)
    {
        var analysis = new Analysis
        {
            Id = Guid.NewGuid(),
            CvText = request.RawCvText,
            JobRequirements = request.JobDescription ?? string.Empty,
            Status = AnalysisStatus.Extracting,
            CreatedAt = DateTime.UtcNow
        };

        await _db.Analyses.AddAsync(analysis, ct);
        await _db.SaveChangesAsync(ct);

        RawExtractedCv extractedData = await _aiExtractor.ExtractVerbatimFactsAsync(request.RawCvText, ct);

        analysis.Status = AnalysisStatus.Tailoring;
        await _db.SaveChangesAsync(ct);

        return new CreateAnalysisResponse(analysis.Id, extractedData);
    }
}