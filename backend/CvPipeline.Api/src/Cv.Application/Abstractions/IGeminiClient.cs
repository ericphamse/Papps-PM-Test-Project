// src/Cv.Application/Abstractions/IGeminiClient.cs
using CvPipeline.Api.Cv.Application.Analysis.ExtractCv;

namespace CvPipeline.Api.Cv.Application.Abstractions;

public interface IGeminiClient
{
    Task<SelectionResult> SelectAsync(
        string cvText,
        string jobRequirements,
        CancellationToken ct,
        List<string>? previousErrors = null);
}