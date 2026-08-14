// src/Cv.Api/Controllers/GenerationsController.cs
using Microsoft.AspNetCore.Mvc;
using CvPipeline.Api.Cv.Application.Generations.CreateGeneration;
using CvPipeline.Api.Cv.Domain;

namespace CvPipeline.Api.Cv.Api.Controllers;

[ApiController]
[Route("api/generations")]
public class GenerationsController : ControllerBase
{
    private readonly CreateGenerationHandler _handler;

    public GenerationsController(CreateGenerationHandler handler) => _handler = handler;

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateGenerationRequest request,
        CancellationToken ct)
    {
        var command = new CreateGenerationCommand(request.AnalysisId, request.Document);
        var result = await _handler.HandleAsync(command, ct);
        return StatusCode(201, result);
    }
}

public record CreateGenerationRequest(Guid AnalysisId, CvDocument Document);