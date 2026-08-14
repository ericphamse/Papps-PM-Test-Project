// src/Cv.Application/Generations/CreateGeneration/Gate2Validator.cs
using CvPipeline.Api.Cv.Application.Normalisation;
using CvPipeline.Api.Cv.Application.Validation;
using CvPipeline.Api.Cv.Domain;

namespace CvPipeline.Api.Cv.Application.Generations.CreateGeneration;

public record Gate2Violation(string Rule, string Message);

public class Gate2Validator
{
    // J2: referees must be present
    // J4: roleSuitability max 200 words
    // J5: 8 detail-table rows structurally present
    // O1: no candidate contact details in generated prose

    public List<Gate2Violation> Validate(CvDocument document)
    {
        var violations = new List<Gate2Violation>();

        // J2 — referees present
        if (document.Referees.Value is null || document.Referees.Value.Count == 0)
            violations.Add(new Gate2Violation("J2", "Referees must be present. At least one referee is required."));

        // J4 — role suitability max 200 words
        if (document.RoleSuitability.Value is not null)
        {
            int wordCount = document.RoleSuitability.Value
                .Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
            if (wordCount > 200)
                violations.Add(new Gate2Violation("J4",
                    $"Role suitability statement is {wordCount} words — maximum is 200."));
        }

        // J5 — 8 detail-table rows structurally present
        // These are guaranteed by the CvDocument type itself (non-nullable FieldValue<T> properties)
        // but we still check they have non-null values
        var missingRows = new List<string>();
        if (document.FullName.Value is null) missingRows.Add("fullName");
        if (document.ProposedRole.Value is null) missingRows.Add("proposedRole");
        if (document.Qualifications.Value is null || document.Qualifications.Value.Count == 0) missingRows.Add("qualifications");
        if (document.SecurityClearance.Value is null) missingRows.Add("securityClearance");
        if (document.YearsOfExperience.Value is null) missingRows.Add("yearsOfExperience");
        if (document.Availability.Value is null) missingRows.Add("availability");
        if (document.Location.Value is null) missingRows.Add("location");
        if (document.Referees.Value is null || document.Referees.Value.Count == 0) missingRows.Add("referees");

        if (missingRows.Count > 0)
            violations.Add(new Gate2Violation("J5",
                $"Missing required detail-table rows: {string.Join(", ", missingRows)}."));

        // O1 — no candidate contact details in generated prose
        // We check professionalProfile and roleSuitability since those are the generated fields
        var generatedText = $"{document.ProfessionalProfile.Value} {document.RoleSuitability.Value}";
        var refereePhones = document.Referees.Value?
            .Select(r => r.Phone)
            .Where(p => !string.IsNullOrWhiteSpace(p)) ?? Enumerable.Empty<string>();
        var contactStrings = RelevanceFilter.ExtractCandidateContactStrings(generatedText, refereePhones);
        var leaks = RelevanceFilter.FindPiiLeaks(generatedText, contactStrings);

        if (leaks.Count > 0)
            violations.Add(new Gate2Violation("O1",
                $"Candidate contact details found in generated prose: {string.Join(", ", leaks.Select(l => $"'{l}'"))}"));

        return violations;
    }
}