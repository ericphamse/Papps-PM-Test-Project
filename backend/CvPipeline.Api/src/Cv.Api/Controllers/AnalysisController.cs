using CvPipeline.Api.Cv.Application.Analysis.ExtractCv;
using Microsoft.AspNetCore.Mvc;

namespace CvPipeline.Api.Cv.Api.Controllers;

[ApiController]
[Route("api/analyses")]
public class AnalysisController : ControllerBase
{
    private readonly CreateAnalysisHandler _handler;

    public AnalysisController(CreateAnalysisHandler handler) => _handler = handler;

    [HttpPost]
    public async Task<IActionResult> Create(
        IFormFile cvFile,
        [FromForm] string jobRequirements,
        CancellationToken ct)
    {
        var result = await _handler.HandleAsync(cvFile, jobRequirements, ct);
        return Ok(result);
    }
}