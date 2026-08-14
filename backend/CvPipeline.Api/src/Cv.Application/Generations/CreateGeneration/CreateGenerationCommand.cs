// src/Cv.Application/Generations/CreateGeneration/CreateGenerationCommand.cs
using CvPipeline.Api.Cv.Domain;

namespace CvPipeline.Api.Cv.Application.Generations.CreateGeneration;

public record CreateGenerationCommand(
    Guid AnalysisId,
    CvDocument Document
);

public record CreateGenerationResult(
    Guid GenerationId,
    string OutputFilename
);