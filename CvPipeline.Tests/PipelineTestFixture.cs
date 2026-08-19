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
            var dbDescriptors = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<CvPipelineDbContext>)
                        || d.ServiceType == typeof(CvPipelineDbContext)
                        || (d.ServiceType.IsGenericType
                            && d.ServiceType.Name.StartsWith("IDbContextOptionsConfiguration")
                            && d.ServiceType.GenericTypeArguments.Contains(typeof(CvPipelineDbContext))))
                .ToList();
            foreach (var d in dbDescriptors)
                services.Remove(d);

            var dbName = "TestDb_" + Guid.NewGuid();
            services.AddDbContext<CvPipelineDbContext>(options =>
                options.UseInMemoryDatabase(dbName));

            var geminiDescriptors = services
                .Where(d => d.ServiceType == typeof(IGeminiClient))
                .ToList();
            foreach (var d in geminiDescriptors)
                services.Remove(d);

            services.AddScoped<IGeminiClient, FakeGeminiClient>();
        });
    }
}