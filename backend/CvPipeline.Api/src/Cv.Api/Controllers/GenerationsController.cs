// src/Cv.Api/Controllers/GenerationsController.cs
using Microsoft.AspNetCore.Mvc;
using CvPipeline.Api.Cv.Application.Generations.CreateGeneration;
using CvPipeline.Api.Cv.Domain;
using CvPipeline.Api.Cv.Infrastructure.Documents;

namespace CvPipeline.Api.Cv.Api.Controllers;

[ApiController]
[Route("api/generations")]
public class GenerationsController : ControllerBase
{
    private readonly CreateGenerationHandler _handler;
    private readonly CvDocumentRenderer _renderer;

     public GenerationsController(CreateGenerationHandler handler, CvDocumentRenderer renderer)
    {
        _handler = handler;
        _renderer = renderer;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateGenerationRequest request,
        CancellationToken ct)
    {
        var command = new CreateGenerationCommand(request.AnalysisId, request.Document);
        var result = await _handler.HandleAsync(command, ct);
        byte[] docxBytes = _renderer.Render(request.Document);
        return File(
            docxBytes,
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            result.OutputFilename);
    }
}

public record CreateGenerationRequest(Guid AnalysisId, CvDocument Document);