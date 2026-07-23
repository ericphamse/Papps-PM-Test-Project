using Microsoft.AspNetCore.Mvc;

namespace CvPipeline.Api.Features.Analyses.CreateAnalysis;

public static class CreateAnalysisEndpoint
{
    public static void MapCreateAnalysisEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/analyses", async (
            CreateAnalysisRequest request,
            CreateAnalysisHandler handler,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.RawCvText))
            {
                return Results.BadRequest(new { error = "RawCvText cannot be empty." });
            }

            var response = await handler.HandleAsync(request, ct);
            return Results.Ok(response);
        })
        .WithName("CreateAnalysis")
        .WithTags("Analyses");
    }
}