using CvPipeline.Api.Data;
using CvPipeline.Api.Cv.Application.Analysis.ExtractCv;
using Microsoft.EntityFrameworkCore;
using CvPipeline.Api.Cv.Application.Abstractions;
using CvPipeline.Api.Cv.Infrastructure.Llm;
using CvPipeline.Api.Cv.Infrastructure.Documents;
using CvPipeline.Api.Cv.Application.Validation;
using CvPipeline.Api.Cv.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddDbContext<CvPipelineDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IGeminiClient, GeminiClient>();
builder.Services.AddScoped<ICvTextExtractor, StubCvTextExtractor>();   // swap to real extractor later
builder.Services.AddScoped<CvDocumentValidator>();
builder.Services.AddScoped<ExtractCvHandler>();
builder.Services.AddScoped<CreateAnalysisHandler>();

var app = builder.Build();   // declared BEFORE any app.Use... / app.Map... calls

app.UseMiddleware<ExceptionMappingMiddleware>();

app.MapControllers();        // replaces MapCreateAnalysisEndpoint — routes AnalysisController instead

app.Run();