// CvPipeline.Tests/PipelineTestFixture.cs
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using CvPipeline.Api.Data;
using CvPipeline.Api.Cv.Application.Abstractions;
using CvPipeline.Api.Cv.Infrastructure.Llm;

namespace CvPipeline.Tests;

public class PipelineTestFixture : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");

        // ← ADD THIS BLOCK FIRST
        builder.ConfigureAppConfiguration(config =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Gemini:ApiKey"] = "fake-key-for-testing",
                ["ConnectionStrings:DefaultConnection"] = "fake"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove ALL DB-related registrations, including the internal
            // per-context IDbContextOptionsConfiguration<T> delegate that
            // AddDbContext adds additively (matched by name since it's an
            // internal EF Core type) — if left in place, the original
            // UseNpgsql(...) callback from Program.cs still runs alongside
            // UseInMemoryDatabase(...) below, which is what causes the
            // "two providers registered" error.
            var dbDescriptors = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<CvPipelineDbContext>)
                        || d.ServiceType == typeof(CvPipelineDbContext)
                        || (d.ServiceType.IsGenericType
                            && d.ServiceType.Name.StartsWith("IDbContextOptionsConfiguration")
                            && d.ServiceType.GenericTypeArguments.Contains(typeof(CvPipelineDbContext))))
                .ToList();
            foreach (var d in dbDescriptors)
                services.Remove(d);

            // Generate the name once here, not inside the options lambda below —
            // DbContextOptions<T> is resolved per-scope, so a Guid.NewGuid() call
            // inside the lambda would hand every request/scope a brand-new, empty
            // in-memory database instead of one shared database for the test run.
            var dbName = "TestDb_" + Guid.NewGuid();
            services.AddDbContext<CvPipelineDbContext>(options =>
                options.UseInMemoryDatabase(dbName));

            // Remove ALL IGeminiClient registrations then add fake
            var geminiDescriptors = services
                .Where(d => d.ServiceType == typeof(IGeminiClient))
                .ToList();
            foreach (var d in geminiDescriptors)
                services.Remove(d);

            services.AddScoped<IGeminiClient, FakeGeminiClient>();
        });
    }
}