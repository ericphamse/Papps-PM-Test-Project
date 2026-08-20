using CvPipeline.Api.Cv.Application.Abstractions;
using CvPipeline.Api.Cv.Application.Validation;

namespace CvPipeline.Api.Cv.Application.Analysis.ExtractCv;

public class SchemaValidationFailedException : Exception
{
    public List<ValidationError> Errors { get; }
    public SchemaValidationFailedException(List<ValidationError> errors)
        : base("Gemini output failed validation after retry.")
        => Errors = errors;
}

public class ExtractCvHandler
{
    private readonly IGeminiClient _gemini;
    private readonly CvDocumentValidator _validator;

    public ExtractCvHandler(IGeminiClient gemini, CvDocumentValidator validator)
    {
        _gemini = gemini;
        _validator = validator;
    }

    public async Task<SelectionResult> RunAsync(string cvText, string jobRequirements, CancellationToken ct)
    {
        var result = await _gemini.SelectAsync(cvText, jobRequirements, ct);
        var errors = _validator.ValidateSelection(result, cvText, jobRequirements);
        if (errors.Count == 0) return result;

        var retryResult = await _gemini.SelectAsync(
            cvText, jobRequirements, ct,
            previousErrors: errors.Select(e => $"{e.FieldPath}: {e.Message}").ToList());

        var retryErrors = _validator.ValidateSelection(retryResult, cvText, jobRequirements);
        if (retryErrors.Count == 0) return retryResult;

        throw new SchemaValidationFailedException(retryErrors);
    }
}