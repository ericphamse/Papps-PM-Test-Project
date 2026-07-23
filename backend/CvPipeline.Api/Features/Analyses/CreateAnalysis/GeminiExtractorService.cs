using System.Text.Json;
using Google.GenAI;
using Google.GenAI.Types;

namespace CvPipeline.Api.Features.Analyses.CreateAnalysis;

public record RawPersonalDetails(
    string? FullName,
    string? Location,
    string? SecurityClearance,
    string? Referees
);

public record RawWorkHistoryItem(
    string? JobTitle,
    string? Company,
    string? Dates,
    List<string>? BulletPoints
);

public record RawEducationItem(
    string? Degree,
    string? Institution
);

public record RawExtractedCv(
    RawPersonalDetails PersonalDetails,
    List<RawWorkHistoryItem> WorkHistory,
    List<RawEducationItem> Education
);

public class GeminiExtractorService
{
    private readonly Client _client;
    private readonly string _modelName;

    public GeminiExtractorService(IConfiguration config)
    {
        var apiKey = config["Gemini:ApiKey"] 
            ?? throw new InvalidOperationException("Gemini:ApiKey is missing in appsettings.json.");
            
        _modelName = config["Gemini:Model"] ?? "gemini-2.5-flash";
        _client = new Client(apiKey: apiKey);
    }

    public async Task<RawExtractedCv> ExtractVerbatimFactsAsync(
        string rawCvText, 
        CancellationToken ct = default)
    {
        var systemInstruction = """
            You are a strict, verbatim CV data extractor for a recruitment pipeline.
            Your job is to read raw text from a candidate's CV and extract facts verbatim into structured JSON.
            
            RULES:
            1. Extract facts EXACTLY as they appear in the raw text.
            2. Do NOT summarize, trim, rewrite, or polish any text or bullet points.
            3. Do NOT fix typos or reformat text in this stage.
            4. If a field is missing in the CV, set its value to null.
            
            You MUST return a JSON object matching this exact property layout:
            {
              "PersonalDetails": {
                "FullName": "...",
                "Location": "...",
                "SecurityClearance": "...",
                "Referees": "..."
              },
              "WorkHistory": [
                {
                  "JobTitle": "...",
                  "Company": "...",
                  "Dates": "...",
                  "BulletPoints": ["..."]
                }
              ],
              "Education": [
                {
                  "Degree": "...",
                  "Institution": "..."
                }
              ]
            }
            """;

        var promptText = $"""
            Extract structured CV facts from the following text:

            ---
            {rawCvText}
            ---
            """;

        var content = new Content
        {
            Parts = new List<Part> { new Part { Text = promptText } }
        };

        var response = await _client.Models.GenerateContentAsync(
            model: _modelName,
            contents: content,
            config: new GenerateContentConfig
            {
                SystemInstruction = new Content 
                { 
                    Parts = new List<Part> { new Part { Text = systemInstruction } } 
                },
                ResponseMimeType = "application/json",
                Temperature = 0.0f
            }
        );

        var jsonText = response.Text;

        if (string.IsNullOrWhiteSpace(jsonText))
        {
            throw new InvalidOperationException("Gemini returned an empty response.");
        }

        string cleanedJson = CleanJsonText(jsonText);

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        try
        {
            return JsonSerializer.Deserialize<RawExtractedCv>(cleanedJson, options)
                ?? throw new InvalidOperationException("Failed to deserialize Gemini output into RawExtractedCv.");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Failed to parse Gemini output as JSON. Raw text: '{jsonText}'", ex);
        }
    }

    private static string CleanJsonText(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "{}";
        
        string trimmed = input.Trim();
        if (trimmed.StartsWith("```"))
        {
            int firstLineEnd = trimmed.IndexOf('\n');
            int lastFence = trimmed.LastIndexOf("```");
            if (firstLineEnd != -1 && lastFence > firstLineEnd)
            {
                trimmed = trimmed.Substring(firstLineEnd + 1, lastFence - firstLineEnd - 1).Trim();
            }
        }
        return trimmed;
    }
}