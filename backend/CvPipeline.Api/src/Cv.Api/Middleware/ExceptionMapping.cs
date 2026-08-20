using CvPipeline.Api.Cv.Application.Analysis.ExtractCv;
using CvPipeline.Api.Cv.Application.Analysis.TailorNarrative;
using CvPipeline.Api.Cv.Application.Generations.CreateGeneration;

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
            context.Response.Headers.Append("Access-Control-Allow-Origin", "http://localhost:3000");
            context.Response.StatusCode = 422;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                error = "schema_validation_failed",
                detail = ex.Errors.Select(e => new { field = e.FieldPath, message = e.Message })
            });
        }
        catch (TailoringValidationException ex)
        {
            context.Response.Headers.Append("Access-Control-Allow-Origin", "http://localhost:3000");
            context.Response.StatusCode = 422;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                error = "tailoring_validation_failed",
                detail = ex.Errors
            });
        }
        catch (Gate2Exception ex)
        {
            context.Response.Headers.Append("Access-Control-Allow-Origin", "http://localhost:3000");
            context.Response.StatusCode = 422;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                error = "gate2_validation_failed",
                violations = ex.Violations.Select(v => new { rule = v.Rule, message = v.Message })
            });
        }
        catch (Google.GenAI.ServerError)
        {
            context.Response.Headers.Append("Access-Control-Allow-Origin", "http://localhost:3000");
            context.Response.StatusCode = 503;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                error = "gemini_unavailable",
                detail = "The AI service is temporarily overloaded. Your input has been saved — please try again shortly."
            });
        }
        catch (TaskCanceledException)
        {
            context.Response.Headers.Append("Access-Control-Allow-Origin", "http://localhost:3000");
            context.Response.StatusCode = 503;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                error = "gemini_timeout",
                detail = "The AI service took too long to respond. Your input has been saved — please try again shortly."
            });
        }
        catch (Exception ex)
        {
            context.Response.Headers.Append("Access-Control-Allow-Origin", "http://localhost:3000");
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                error = "internal_error",
                detail = ex.Message,
                inner = ex.InnerException?.Message,        // ← add this
                innerInner = ex.InnerException?.InnerException?.Message   // ← and this
            });
        }
        
    }
}