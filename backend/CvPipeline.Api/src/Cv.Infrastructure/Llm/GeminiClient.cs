using System.Text.Json;
using Google.GenAI;
using Google.GenAI.Types;
using CvPipeline.Api.Cv.Application.Abstractions;
using CvPipeline.Api.Cv.Application.Analysis.ExtractCv;
using CvPipeline.Api.Cv.Domain;
using CvPipeline.Api.Cv.Application.Analysis.TailorNarrative;

namespace CvPipeline.Api.Cv.Infrastructure.Llm;

public class GeminiClient : IGeminiClient
{
    private readonly Client _client;
    private readonly string _modelName;

    public GeminiClient(IConfiguration config)
    {
        var apiKey = config["Gemini:ApiKey"]
            ?? throw new InvalidOperationException("Gemini:ApiKey is missing.");
        _modelName = config["Gemini:Model"] ?? "gemini-3.6-flash";
        _client = new Client(apiKey: apiKey, httpOptions: new HttpOptions { Timeout = 180000 });
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
                Temperature = 0.0f,
                HttpOptions = new HttpOptions { Timeout = 180000 }
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
            "CommendationsAndAwards": [
            { "Description": {"SourceQuotes":["..."]}, "Year": {"SourceQuotes":["..."]} }
          ],
          "QualificationsDetailed": [
            { "Qualification": {"SourceQuotes":["..."]}, "Institution": {"SourceQuotes":["..."]},
              "Year": {"SourceQuotes":["..."]} }
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

    public async Task<TailoredFields> TailorAsync(
        CvDocument partial,
        string jobRequirements,
        List<string> keptTechnologies,
        CancellationToken ct,
        List<string>? previousErrors = null)
    {
        string systemInstruction = BuildTailorSystemInstruction();
        string promptText = BuildTailorPromptText(partial, jobRequirements, keptTechnologies, previousErrors);

        var response = await _client.Models.GenerateContentAsync(
            model: _modelName,
            contents: new Content { Parts = new List<Part> { new Part { Text = promptText } } },
            config: new GenerateContentConfig
            {
                SystemInstruction = new Content { Parts = new List<Part> { new Part { Text = systemInstruction } } },
                ResponseMimeType = "application/json",
                Temperature = 0.4f,
                HttpOptions = new HttpOptions { Timeout = 180000 }
            }
        );

        string? jsonText = response.Text;
        if (string.IsNullOrWhiteSpace(jsonText))
            throw new InvalidOperationException("Gemini returned an empty response for stage 3.");

        string cleaned = CleanJsonText(jsonText);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        return JsonSerializer.Deserialize<TailoredFields>(cleaned, options)
            ?? throw new InvalidOperationException("Failed to deserialize stage 3 response.");
    }

    private static string BuildTailorSystemInstruction() => """
        You are a professional CV tailoring assistant for a recruitment pipeline.
        You have been given a candidate's verified CV facts (already extracted and normalised)
        and a job requirements document (RFQ). Your job is to write exactly 5 fields of
        a tailored CV document.

        RULES:
        1. Every claim you make must be grounded in the verified CV facts provided.
        Do NOT invent experience, skills, or achievements the CV does not support.
        2. professionalProfile: impersonal, no pronouns, no name. 40-150 words.
        3. roleSuitability: third person. Use the candidate's name, then "they". Maximum 200 words.
        Must explicitly reference E1-E6 criteria from the job requirements.
        4. yearsOfExperience: combine the years figure from the CV with a 2-3 part taxonomy
        drawn from what the job requirements ask for.
        Format: "{years}+ years - {area1}, {area2}, {area3}".
        5. coreCompetencies: exactly 10 bullet strings. Each must cite at least one criterion
        from the job requirements (E1-E6 or desirable criteria).
        Collectively they must cover E2, E3, E5 and E6.
        Bullets 1-4 should cover essential criteria. Bullets 5-8 desirable criteria.
        Bullets 9-10 should cover E5 (leadership) and E6 (communication).
        6. careerHighlights: exactly 6 entries. Each has a heading and a bullet point.
        Each must cite at least one criterion. Collectively cover E2, E3, E4 and E5.
        Bullet text must be verb-first, third-person implied (e.g. "Led the migration...").
        7. No first person (I, me, my, myself) anywhere in any field.
        8. Use Australian English (modernising, containerisation, prioritise).
        9. Return ONLY valid JSON matching the schema below. No markdown fences, no commentary.

        Schema:
        {
        "ProfessionalProfile": "...",
        "RoleSuitability": "...",
        "YearsOfExperience": "...",
        "CoreCompetencies": [
            { "Text": "...", "Criteria": ["E2", "E3"] }
        ],
        "CareerHighlights": [
            { "Heading": "...", "Bullet": "...", "Criteria": ["E2", "E4"] }
        ]
        }
        """;

    private static string BuildTailorPromptText(
        CvDocument partial, string jobRequirements,
        List<string> keptTechnologies, List<string>? previousErrors)
    {
        var facts = new System.Text.StringBuilder();

        facts.AppendLine($"Candidate name: {partial.FullName.Value}");
        facts.AppendLine($"Location: {partial.Location.Value}");
        facts.AppendLine($"Availability: {partial.Availability.Value}");
        facts.AppendLine($"Security clearance: {partial.SecurityClearance.Value ?? "Not stated"}");
        facts.AppendLine($"Years of experience (raw): {partial.YearsOfExperience.Value ?? "About 15 years"}");

        facts.AppendLine("\nCareer synopsis:");
        if (partial.CareerSynopsis.Value is not null)
            foreach (var entry in partial.CareerSynopsis.Value)
                facts.AppendLine($"  - {entry.Title} at {entry.Organisation} ({entry.StartYear} - {entry.EndYear})");

        facts.AppendLine("\nQualifications:");
        if (partial.Qualifications.Value is not null)
            foreach (var q in partial.Qualifications.Value)
                facts.AppendLine($"  - {q}");

        facts.AppendLine("\nRelevant technologies (verified against job requirements):");
        foreach (var tech in keptTechnologies)
            facts.AppendLine($"  - {tech}");

        facts.AppendLine("\nCommendations and awards:");
        if (partial.CommendationsAndAwards.Value is not null)
            foreach (var c in partial.CommendationsAndAwards.Value)
                facts.AppendLine($"  - {c.Description} ({c.Year})");

        string retryNote = previousErrors is { Count: > 0 }
            ? $"\n\nYour previous response had these errors — fix them:\n{string.Join("\n", previousErrors)}"
            : "";

        return $"""
            Verified CV facts:
            ---
            {facts}
            ---

            Job requirements:
            ---
            {jobRequirements}
            ---
            {retryNote}
            """;
    }
    }