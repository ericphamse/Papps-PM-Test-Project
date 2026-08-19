using CvPipeline.Api.Cv.Application.Normalisation;
using CvPipeline.Api.Cv.Application.Validation;
using CvPipeline.Api.Cv.Domain;

namespace CvPipeline.Api.Cv.Application.Generations.CreateGeneration;

public record Gate2Violation(string Rule, string Message);

public class Gate2Validator
{
    public List<Gate2Violation> Validate(CvDocument document)
    {
        var violations = new List<Gate2Violation>();

        if (document.Referees.Value is null || document.Referees.Value.Count == 0)
            violations.Add(new Gate2Violation("J2", "Referees must be present. At least one referee is required."));

        if (document.RoleSuitability.Value is not null)
        {
            int wordCount = document.RoleSuitability.Value
                .Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
            if (wordCount > 200)
                violations.Add(new Gate2Violation("J4",
                    $"Role suitability statement is {wordCount} words — maximum is 200."));
        }

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