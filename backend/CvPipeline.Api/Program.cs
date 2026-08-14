using CvPipeline.Api.Data;
using CvPipeline.Api.Cv.Application.Analysis.ExtractCv;
using Microsoft.EntityFrameworkCore;
using CvPipeline.Api.Cv.Application.Abstractions;
using CvPipeline.Api.Cv.Infrastructure.Llm;
using CvPipeline.Api.Cv.Infrastructure.Documents;
using CvPipeline.Api.Cv.Application.Validation;
using CvPipeline.Api.Cv.Api.Middleware;
using CvPipeline.Api.Cv.Application.Analysis.TailorNarrative;
using CvPipeline.Api.Cv.Application.Generations.CreateGeneration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.MaxDepth = 128;
        options.JsonSerializerOptions.ReferenceHandler = 
            System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

builder.Services.AddDbContext<CvPipelineDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IGeminiClient, GeminiClient>();
builder.Services.AddScoped<ICvTextExtractor, StubCvTextExtractor>();
builder.Services.AddScoped<CvDocumentValidator>();
builder.Services.AddScoped<ExtractCvHandler>();
builder.Services.AddScoped<CreateAnalysisHandler>();
builder.Services.AddScoped<NormaliseSelectionHandler>();
builder.Services.AddScoped<TailorNarrativeHandler>();
builder.Services.AddScoped<Gate2Validator>();
builder.Services.AddScoped<CreateGenerationHandler>();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod());
});


var app = builder.Build();

app.UseCors();
app.UseMiddleware<ExceptionMappingMiddleware>();
app.MapControllers();

app.Run();