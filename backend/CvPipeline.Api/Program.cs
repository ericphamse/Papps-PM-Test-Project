/**using CvPipeline.Api.Data;
using CvPipeline.Api.Features.Analyses.CreateAnalysis;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddDbContext<CvPipelineDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<CreateAnalysisHandler>();
builder.Services.AddScoped<GeminiExtractorService>();

var app = builder.Build();

app.MapPost("/api/test-gemini", async (GeminiExtractorService extractor) =>
{
    var sampleCv = """
        Jordan Reeve
        Adelaide, SA, 20410, Australia
        Security Clearance: Old National Police Check at 2010, ditmemay

        WORK HISTORY:
        Senior Software Engineer at TechCorp Australia (2021-03 to Present)
        - Architected microservices handling 2M requests daily.
        - Engineered backend pipelines using C# and EF Core.

        EDUCATION:
        B.S. Computer Science - University of Adelaide
        """;

    var result = await extractor.ExtractVerbatimFactsAsync(sampleCv);
    return Results.Ok(result);
});

app.UseHttpsRedirection();
app.MapControllers();

app.Run();**/

using CvPipeline.Api.Data;
using CvPipeline.Api.Features.Analyses.CreateAnalysis;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<CvPipelineDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<GeminiExtractorService>();
builder.Services.AddScoped<CreateAnalysisHandler>();

var app = builder.Build();

app.MapCreateAnalysisEndpoint();

app.Run();