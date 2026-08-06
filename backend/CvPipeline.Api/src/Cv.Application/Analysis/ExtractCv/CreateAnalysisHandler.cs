using CvPipeline.Api.Data;
using CvPipeline.Api.Models;
using CvPipeline.Api.Cv.Domain;
using CvPipeline.Api.Cv.Infrastructure.Documents;
using CvPipeline.Api.Cv.Application.Analysis.ParseJobRequirements;

namespace CvPipeline.Api.Cv.Application.Analysis.ExtractCv;

public class CreateAnalysisHandler
{
    private readonly CvPipelineDbContext _db;
    private readonly ICvTextExtractor _extractor;
    private readonly ExtractCvHandler _extractCv;
    private readonly NormaliseSelectionHandler _normaliser; 

    public CreateAnalysisHandler(CvPipelineDbContext db, ICvTextExtractor extractor, ExtractCvHandler extractCv, NormaliseSelectionHandler normaliser)
    {
        _db = db;
        _extractor = extractor;
        _extractCv = extractCv;
        _normaliser = normaliser;
    }

    public async Task<CreateAnalysisResult> HandleAsync(
        IFormFile cvFile,
        string jobRequirementsText,
        CancellationToken ct)
    {
        //Extract CV text
        string cvText = await _extractor.ExtractTextAsync(cvFile, ct);

        //PERSIST FIRST
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

        //Parse JD
        var parsedJd = JobRequirementsParser.Parse(jobRequirementsText);
        //Stage1
        var selection = await _extractCv.RunAsync(cvText, jobRequirementsText, ct);
        //Stage2
        var CvDocument = _normaliser.Normalise(selection, parsedJd, cvText, jobRequirementsText);

        return new CreateAnalysisResult(analysis.Id, parsedJd, selection, CvDocument);
    }
}

public record CreateAnalysisResult(Guid AnalysisId, ParsedJobRequirements ParsedJd, SelectionResult Selection, CvDocument CvDocument);