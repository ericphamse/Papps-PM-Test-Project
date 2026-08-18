// CvPipeline.Tests/PipelineSuiteTests.cs
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using CvPipeline.Api.Data;
using Xunit;

namespace CvPipeline.Tests;

public class PipelineSuiteTests : IClassFixture<PipelineTestFixture>
{
    private readonly HttpClient _client;
    private readonly PipelineTestFixture _factory;

    // Fixture file paths
    private const string CvFixture = "fixtures/Jordan_Reeve_Resume_ORIGINAL.txt";
    private const string JdFixture = "fixtures/JD_Senior_Software_Engineer_Level_4.txt";

    public PipelineSuiteTests(PipelineTestFixture factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<(HttpResponseMessage Response, JsonDocument Json)> PostAnalysis()
    {
        var cvText = await File.ReadAllTextAsync(CvFixture);
        var jdText = await File.ReadAllTextAsync(JdFixture);

        // Strip null bytes — same fix as in CreateAnalysisHandler
        cvText = cvText.Replace("\0", "");
        jdText = jdText.Replace("\0", "");

        var form = new MultipartFormDataContent();
        form.Add(new StringContent(cvText), "cvFile", "Jordan_Reeve_Resume_ORIGINAL.txt");
        form.Add(new StringContent(jdText), "jobRequirements");

        var response = await _client.PostAsync("/api/analyses", form);
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return (response, json);
    }

    // G1: returns 200
    [Fact]
    public async Task G1_PostAnalyses_Returns200()
    {
        var (response, _) = await PostAnalysis();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // G2: analyses row persisted before Gemini call
    [Fact]
    public async Task G2_AnalysisRow_PersistedBeforeGeminiCall()
    {
        var (response, json) = await PostAnalysis();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var analysisId = json.RootElement.GetProperty("analysisId").GetString();
        Assert.NotNull(analysisId);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CvPipelineDbContext>();
        var row = await db.Analyses.FindAsync(Guid.Parse(analysisId!));
        Assert.NotNull(row);
    }

    // G3: roleTitle = "Senior Software Engineer"
    [Fact]
    public async Task G3_ParsedJd_RoleTitle_Correct()
    {
        var (_, json) = await PostAnalysis();
        var roleTitle = json.RootElement
            .GetProperty("parsedJd")
            .GetProperty("roleTitle")
            .GetProperty("value")
            .GetString();
        Assert.Equal("Senior Software Engineer", roleTitle);
    }

    // G4: level = "Level 4"
    [Fact]
    public async Task G4_ParsedJd_Level_Correct()
    {
        var (_, json) = await PostAnalysis();
        var level = json.RootElement
            .GetProperty("parsedJd")
            .GetProperty("level")
            .GetProperty("value")
            .GetString();
        Assert.Equal("Level 4", level);
    }

    // G5: fullName sourceQuotes contains "Jordan Reeve"
    [Fact]
    public async Task G5_Selection_FullName_ContainsJordanReeve()
    {
        var (_, json) = await PostAnalysis();
        var quotes = json.RootElement
            .GetProperty("tailoredDocument")
            .GetProperty("fullName")
            .GetProperty("sourceQuotes")
            .EnumerateArray()
            .Select(q => q.GetString())
            .ToList();
        Assert.Contains("Jordan Reeve", quotes);
    }

    // G6: location sourceQuotes contains "Adelaide SA 5000"
    [Fact]
    public async Task G6_Selection_Location_ContainsAdelaideSA5000()
    {
        var (_, json) = await PostAnalysis();
        var quotes = json.RootElement
            .GetProperty("tailoredDocument")
            .GetProperty("location")
            .GetProperty("sourceQuotes")
            .EnumerateArray()
            .Select(q => q.GetString())
            .ToList();
        Assert.Contains("Adelaide SA 5000", quotes);
    }

    // G7: careerSynopsis has 8 rows
    [Fact]
    public async Task G7_CareerSynopsis_Has8Rows()
    {
        var (_, json) = await PostAnalysis();
        var career = json.RootElement
            .GetProperty("tailoredDocument")
            .GetProperty("careerSynopsis")
            .GetProperty("value")
            .EnumerateArray()
            .ToList();
        Assert.Equal(8, career.Count);
    }

    // G8: bar attendant absent from careerSynopsis
    [Fact]
    public async Task G8_BarAttendant_AbsentFromCareerSynopsis()
    {
        var (_, json) = await PostAnalysis();
        var titles = json.RootElement
            .GetProperty("tailoredDocument")
            .GetProperty("careerSynopsis")
            .GetProperty("value")
            .EnumerateArray()
            .Select(e => e.GetProperty("title").GetString()?.ToLowerInvariant())
            .ToList();
        Assert.DoesNotContain(titles, t => t != null && t.Contains("bar attendant"));
    }

    // G9: Adelaide High School absent from qualificationsDetailed
    [Fact]
    public async Task G9_AdelaideHighSchool_AbsentFromQualifications()
    {
        var (_, json) = await PostAnalysis();
        var quals = json.RootElement
            .GetProperty("tailoredDocument")
            .GetProperty("qualificationsDetailed")
            .GetProperty("value")
            .EnumerateArray()
            .Select(q => q.GetProperty("qualification").GetString()?.ToLowerInvariant())
            .ToList();
        Assert.DoesNotContain(quals, q => q != null && q.Contains("high school"));
    }

    // G10: location.value = "Adelaide, SA" (N1 applied)
    [Fact]
    public async Task G10_Location_Value_NormalisedByN1()
    {
        var (_, json) = await PostAnalysis();
        var location = json.RootElement
            .GetProperty("tailoredDocument")
            .GetProperty("location")
            .GetProperty("value")
            .GetString();
        Assert.Equal("Adelaide, SA", location);
    }

    // G11: every competency bullet cites at least one criterion
    [Fact]
    public async Task G11_CoreCompetencies_EachCitesAtLeastOneCriterion()
    {
        var (_, json) = await PostAnalysis();
        var competencies = json.RootElement
            .GetProperty("tailoredDocument")
            .GetProperty("coreCompetencies")
            .GetProperty("value")
            .EnumerateArray()
            .ToList();

        Assert.Equal(10, competencies.Count);

        foreach (var c in competencies)
        {
            var criteria = c.GetProperty("criteria").EnumerateArray().ToList();
            Assert.True(criteria.Count > 0, $"Competency '{c.GetProperty("text").GetString()}' has no criteria");
        }
    }
}