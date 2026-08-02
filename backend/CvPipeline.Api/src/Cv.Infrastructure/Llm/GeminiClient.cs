// src/Cv.Infrastructure/Llm/GeminiClient.cs
using System.Text.Json;
using Google.GenAI;
using Google.GenAI.Types;
using CvPipeline.Api.Cv.Application.Abstractions;
using CvPipeline.Api.Cv.Application.Analysis.ExtractCv;

namespace CvPipeline.Api.Cv.Infrastructure.Llm;

public class GeminiClient : IGeminiClient
{
    private readonly Client _client;
    private readonly string _modelName;

    public GeminiClient(IConfiguration config)
    {
        var apiKey = config["Gemini:ApiKey"]
            ?? throw new InvalidOperationException("Gemini:ApiKey is missing.");
        _modelName = config["Gemini:Model"] ?? "gemini-2.5-flash";
        _client = new Client(apiKey: apiKey);
    }

    public async Task<SelectionResult> SelectAsync(
        string cvText,
        string jobRequirements,
        CancellationToken ct,
        List<string>? previousErrors = null)
    {
        string systemInstruction = BuildSystemInstruction();
        string promptText = BuildPromptText(cvText, jobRequirements, previousErrors);

        var response = await _client.Models.GenerateContentAsync(
            model: _modelName,
            contents: new Content { Parts = new List<Part> { new Part { Text = promptText } } },
            config: new GenerateContentConfig
            {
                SystemInstruction = new Content { Parts = new List<Part> { new Part { Text = systemInstruction } } },
                ResponseMimeType = "application/json",
                Temperature = 0.0f
            }
        );

        string? jsonText = response.Text;
        if (string.IsNullOrWhiteSpace(jsonText))
            throw new InvalidOperationException("Gemini returned an empty response.");

        string cleaned = CleanJsonText(jsonText);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        return JsonSerializer.Deserialize<SelectionResult>(cleaned, options)
            ?? throw new InvalidOperationException("Failed to deserialize Gemini output into SelectionResult.");
    }

    private static string BuildSystemInstruction() => """
        You are a strict span-selection assistant for a recruitment pipeline.
        Given a candidate's CV text and a job requirements document (RFQ), your job is
        NOT to extract clean values and NOT to rewrite anything. You must:

        1. For each field below, find the EXACT text span in the CV that supports it,
           character-for-character. Do not paraphrase, shorten, or reformat the span.
        2. If a field is not supported anywhere in the CV, return an empty array for it.
        3. For every distinct technology/tool/skill mentioned in the CV, decide keep or drop:
           - keep=true only if the job requirements text supports its relevance. Quote the
             EXACT span of the job requirements text that justifies the keep, verbatim.
           - keep=false if not supported by the job requirements. Provide no quote.
        4. Do NOT invent a span that does not exist verbatim in the source text.
        5. Return ONLY JSON matching the schema below. No commentary, no markdown fences.

        Schema:
        {
          "FullName": { "SourceQuotes": ["..."] },
          "Qualifications": { "SourceQuotes": ["..."] },
          "SecurityClearance": { "SourceQuotes": ["..."] },
          "YearsOfExperience": { "SourceQuotes": ["..."] },
          "Availability": { "SourceQuotes": ["..."] },
          "Location": { "SourceQuotes": ["..."] },
          "Referees": [
            { "Name": {"SourceQuotes":["..."]}, "Position": {"SourceQuotes":["..."]},
              "Organisation": {"SourceQuotes":["..."]}, "Phone": {"SourceQuotes":["..."]} }
          ],
          "CareerSynopsis": [
            { "Title": {"SourceQuotes":["..."]}, "Organisation": {"SourceQuotes":["..."]},
              "StartYear": {"SourceQuotes":["..."]}, "EndYear": {"SourceQuotes":["..."]} }
          ],
          "TechVotes": [
            { "Technology": "...", "Keep": true, "RfqQuote": "..." }
          ]
        }
        """;

    private static string BuildPromptText(string cvText, string jobRequirements, List<string>? previousErrors)
    {
        string retryNote = previousErrors is { Count: > 0 }
            ? $"\n\nYour previous response had these errors — fix them:\n{string.Join("\n", previousErrors)}"
            : "";

        return $"""
            CV text:
            ---
            {cvText}
            ---

            Job requirements text:
            ---
            {jobRequirements}
            ---
            {retryNote}
            """;
    }

    private static string CleanJsonText(string input)
    {
        string trimmed = input.Trim();
        if (trimmed.StartsWith("```"))
        {
            int firstLineEnd = trimmed.IndexOf('\n');
            int lastFence = trimmed.LastIndexOf("```");
            if (firstLineEnd != -1 && lastFence > firstLineEnd)
                trimmed = trimmed.Substring(firstLineEnd + 1, lastFence - firstLineEnd - 1).Trim();
        }
        return trimmed;
    }
}