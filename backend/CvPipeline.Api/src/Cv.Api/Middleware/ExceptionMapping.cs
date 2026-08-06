// src/Cv.Api/Middleware/ExceptionMapping.cs
using CvPipeline.Api.Cv.Application.Analysis.ExtractCv;

namespace CvPipeline.Api.Cv.Api.Middleware;

public class ExceptionMappingMiddleware
{
    private readonly RequestDelegate _next;
    public ExceptionMappingMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (SchemaValidationFailedException ex)
        {
            context.Response.StatusCode = 422;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                error = "schema_validation_failed",
                detail = ex.Errors.Select(e => new { field = e.FieldPath, message = e.Message })
            });
        }
        catch (Google.GenAI.ServerError ex)   // NEW
        {
            context.Response.StatusCode = 503;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "gemini_unavailable",
                detail = "The AI service is temporarily overloaded. Your CV and job requirements have already been saved — please try again in a minute."
            });
        }
    }
}