using CvPipeline.Api.Data;
using CvPipeline.Api.Models;
using CvPipeline.Api.Cv.Infrastructure.Documents;
using CvPipeline.Api.Cv.Application.Analysis.ParseJobRequirements;

namespace CvPipeline.Api.Cv.Application.Analysis.ExtractCv;

public class CreateAnalysisHandler
{
    private readonly CvPipelineDbContext _db;
    private readonly ICvTextExtractor _extractor;
    private readonly ExtractCvHandler _extractCv;

    public CreateAnalysisHandler(CvPipelineDbContext db, ICvTextExtractor extractor, ExtractCvHandler extractCv)
    {
        _db = db;
        _extractor = extractor;
        _extractCv = extractCv;
    }

    public async Task<CreateAnalysisResult> HandleAsync(
        IFormFile cvFile,
        string jobRequirementsText,
        CancellationToken ct)
    {
        // 1. Extract CV text
        string cvText = await _extractor.ExtractTextAsync(cvFile, ct);

        // 2. PERSIST FIRST — before any Gemini call
        var analysis = new CvPipeline.Api.Models.Analysis
        {   
            Id = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            CvText = cvText,
            JobRequirements = jobRequirementsText,
            CvSourceFilename = cvFile.FileName,
            Status = AnalysisStatus.Extracting
        };
        _db.Analyses.Add(analysis);
        await _db.SaveChangesAsync(ct);

        // 3. Parse JD — pure C#, already built
        var parsedJd = JobRequirementsParser.Parse(jobRequirementsText);
        //Stage1
        var selection = await _extractCv.RunAsync(cvText, jobRequirementsText, ct);

        return new CreateAnalysisResult(analysis.Id, parsedJd, selection);
    }
}

public record CreateAnalysisResult(Guid AnalysisId, ParsedJobRequirements ParsedJd, SelectionResult Selection);