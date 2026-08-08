using CvPipeline.Api.Data;
using CvPipeline.Api.Models;
using CvPipeline.Api.Cv.Domain;
using CvPipeline.Api.Cv.Infrastructure.Documents;
using CvPipeline.Api.Cv.Application.Analysis.ParseJobRequirements;
using CvPipeline.Api.Cv.Application.Analysis.TailorNarrative;
using System.Text.Json;

namespace CvPipeline.Api.Cv.Application.Analysis.ExtractCv;

public class CreateAnalysisHandler
{
    private readonly CvPipelineDbContext _db;
    private readonly ICvTextExtractor _extractor;
    private readonly ExtractCvHandler _extractCv;
    private readonly NormaliseSelectionHandler _normaliser;
    private readonly TailorNarrativeHandler _tailor;

    public CreateAnalysisHandler(CvPipelineDbContext db, ICvTextExtractor extractor, ExtractCvHandler extractCv, NormaliseSelectionHandler normaliser, TailorNarrativeHandler tailor)
    {
        _db = db;
        _extractor = extractor;
        _extractCv = extractCv;
        _normaliser = normaliser;
        _tailor = tailor;
    }

    public async Task<CreateAnalysisResult> HandleAsync(
        IFormFile cvFile,
        string jobRequirementsText,
        CancellationToken ct)
    {
        //Extract cv text
        string cvText = await _extractor.ExtractTextAsync(cvFile, ct);

        //Save to DB
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

        //Parse jd
        var parsedJd = JobRequirementsParser.Parse(jobRequirementsText);
        //stage1
        var selection = await _extractCv.RunAsync(cvText, jobRequirementsText, ct);
        //stage2
        var normalisedResult = _normaliser.Normalise(selection, parsedJd, cvText, jobRequirementsText);
        var partialDocument = normalisedResult.Document;
        var keptTechnologies = normalisedResult.KeptTechnologies;

        //stage3
        analysis.Status = AnalysisStatus.Tailoring;
        await _db.SaveChangesAsync(ct);
        var tailoredDocument = await _tailor.RunAsync(partialDocument, jobRequirementsText, keptTechnologies, cvText, ct);

        //persist final document
        analysis.Status = "review";
        analysis.Document = JsonDocument.Parse(
            System.Text.Json.JsonSerializer.Serialize(tailoredDocument));
        await _db.SaveChangesAsync(ct);
        return new CreateAnalysisResult(analysis.Id, parsedJd, tailoredDocument);
    }
}

public record CreateAnalysisResult(Guid AnalysisId, ParsedJobRequirements ParsedJd, CvDocument tailoredDocument);