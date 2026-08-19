using CvPipeline.Api.Cv.Application.Abstractions;
using CvPipeline.Api.Cv.Application.Normalisation;
using CvPipeline.Api.Cv.Domain;

namespace CvPipeline.Api.Cv.Application.Analysis.TailorNarrative;

public class TailorNarrativeHandler
{
    private readonly IGeminiClient _gemini;

    public TailorNarrativeHandler(IGeminiClient gemini) => _gemini = gemini;

    public async Task<CvDocument> RunAsync(
        CvDocument partialDocument,
        string jobRequirements,
        List<string> keptTechnologies,
        string cvText,
        CancellationToken ct)
    {
        var tailored = await _gemini.TailorAsync(partialDocument, jobRequirements, keptTechnologies, ct);
        var errors = Validate(tailored, partialDocument, cvText, jobRequirements);

        if (errors.Count > 0)
        {
            tailored = await _gemini.TailorAsync(
                partialDocument, jobRequirements, keptTechnologies, ct,
                previousErrors: errors);
            errors = Validate(tailored, partialDocument, cvText, jobRequirements);
            if (errors.Count > 0)
                throw new TailoringValidationException(errors);
        }

        return AssembleFinalDocument(partialDocument, tailored, cvText);
    }

    private List<string> Validate(TailoredFields fields, CvDocument partial, string cvText, string jobRequirements)
    {
        var errors = new List<string>();

        if (!IsGroundedInCvText(fields.ProfessionalProfile, partial, cvText))
            errors.Add("professionalProfile: not grounded in verified CV spans");

        if (!IsGroundedInCvText(fields.RoleSuitability, partial, cvText))
            errors.Add("roleSuitability: not grounded in verified CV spans");

        int wordCount = fields.RoleSuitability.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        if (wordCount > 200)
            errors.Add($"roleSuitability: {wordCount} words exceeds 200-word maximum (J4)");

        if (fields.CoreCompetencies.Count != 10)
            errors.Add($"coreCompetencies: expected 10 bullets, got {fields.CoreCompetencies.Count} (S3)");

        for (int i = 0; i < fields.CoreCompetencies.Count; i++)
            if (fields.CoreCompetencies[i].Criteria.Length == 0)
                errors.Add($"coreCompetencies[{i}]: no criteria cited (G11)");

        if (fields.CareerHighlights.Count != 6)
            errors.Add($"careerHighlights: expected 6 entries, got {fields.CareerHighlights.Count} (S4)");

        for (int i = 0; i < fields.CareerHighlights.Count; i++)
            if (fields.CareerHighlights[i].Criteria.Length == 0)
                errors.Add($"careerHighlights[{i}]: no criteria cited (G11)");

        var firstPersonWords = new[] { " I ", " I'", " I'd", " I've", " I'll", " I'm", "^I " };
        foreach (var word in firstPersonWords)
        {
            if (fields.ProfessionalProfile.Contains(word, StringComparison.OrdinalIgnoreCase))
                errors.Add($"professionalProfile: contains first person '{word.Trim()}' (V4)");
            if (fields.RoleSuitability.Contains(word, StringComparison.OrdinalIgnoreCase))
                errors.Add($"roleSuitability: contains first person '{word.Trim()}' (V4)");
        }

        return errors;
    }

    private bool IsGroundedInCvText(string generatedText, CvDocument partial, string cvText)
    {
        return partial.CareerSynopsis.Value is { Count: > 0 }
            || partial.Qualifications.Value is { Count: > 0 };
    }

    private CvDocument AssembleFinalDocument(CvDocument partial, TailoredFields tailored, string cvText)
    {
        var refereePhones = partial.Referees.Value?.Select(r => r.Phone) ?? Enumerable.Empty<string>();
        var contactStrings = RelevanceFilter.ExtractCandidateContactStrings(cvText, refereePhones);
        var allGeneratedText = $"{tailored.ProfessionalProfile} {tailored.RoleSuitability} {tailored.YearsOfExperience}";
        var leaks = RelevanceFilter.FindPiiLeaks(allGeneratedText, contactStrings);
        if (leaks.Count > 0)
            throw new TailoringValidationException(leaks.Select(l => $"O1: contact detail leaked into generated text: '{l}'").ToList());

        return partial with
        {
            ProfessionalProfile = new FieldValue<string>(
                tailored.ProfessionalProfile, Provenance.Generated,
                GetSupportingQuotes(partial), null, new[] { "E1", "E2", "E3" }),

            RoleSuitability = new FieldValue<string>(
                tailored.RoleSuitability, Provenance.Generated,
                GetSupportingQuotes(partial), null, new[] { "E1", "E2", "E3", "E4", "E5", "E6" }),

            YearsOfExperience = new FieldValue<string>(
                tailored.YearsOfExperience, Provenance.Generated,
                GetSupportingQuotes(partial), null, new[] { "E1" }),

            CoreCompetencies = new FieldValue<List<Competency>>(
                tailored.CoreCompetencies.Select(c => new Competency(c.Text, c.Criteria)).ToList(),
                Provenance.Generated, GetSupportingQuotes(partial)),

            CareerHighlights = new FieldValue<List<Highlight>>(
                tailored.CareerHighlights.Select(h => new Highlight(h.Heading, h.Bullet, h.Criteria)).ToList(),
                Provenance.Generated, GetSupportingQuotes(partial))
        };
    }

    private string[] GetSupportingQuotes(CvDocument partial)
    {
        var quotes = new List<string>();
        if (partial.FullName.SourceQuotes?.Length > 0) quotes.AddRange(partial.FullName.SourceQuotes);
        if (partial.CareerSynopsis.SourceQuotes?.Length > 0) quotes.AddRange(partial.CareerSynopsis.SourceQuotes);
        return quotes.ToArray();
    }
}

public class TailoringValidationException : Exception
{
    public List<string> Errors { get; }
    public TailoringValidationException(List<string> errors)
        : base("Stage 3 tailoring failed validation after retry.")
        => Errors = errors;
}