// src/Cv.Application/Abstractions/IGeminiClient.cs
using CvPipeline.Api.Cv.Application.Analysis.ExtractCv;
using CvPipeline.Api.Cv.Application.Analysis.TailorNarrative;
using CvPipeline.Api.Cv.Domain;

namespace CvPipeline.Api.Cv.Application.Abstractions;

public interface IGeminiClient
{
    Task<SelectionResult> SelectAsync(
        string cvText,
        string jobRequirements,
        CancellationToken ct,
        List<string>? previousErrors = null);

    Task<TailoredFields> TailorAsync(
        CvDocument partialDocument,
        string jobRequirements,
        List<string> keptTechnologies,
        CancellationToken ct,
        List<string>? previousErrors = null);
}