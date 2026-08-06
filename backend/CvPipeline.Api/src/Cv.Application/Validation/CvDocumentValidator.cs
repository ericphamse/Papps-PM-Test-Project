// src/Cv.Application/Validation/CvDocumentValidator.cs
using CvPipeline.Api.Cv.Application.Analysis.ExtractCv;
using CvPipeline.Api.Cv.Domain;

namespace CvPipeline.Api.Cv.Application.Validation;

public record ValidationError(string FieldPath, string Message);

public class CvDocumentValidator
{
    public List<ValidationError> ValidateSelection(SelectionResult result, string cvText, string jobRequirements)
    {
        var errors = new List<ValidationError>();

        CheckQuotes(result.FullName, cvText, nameof(result.FullName), errors);
        CheckQuotes(result.Qualifications, cvText, nameof(result.Qualifications), errors);
        CheckQuotes(result.SecurityClearance, cvText, nameof(result.SecurityClearance), errors);
        CheckQuotes(result.YearsOfExperience, cvText, nameof(result.YearsOfExperience), errors);
        CheckQuotes(result.Availability, cvText, nameof(result.Availability), errors);
        CheckQuotes(result.Location, cvText, nameof(result.Location), errors);

        for (int i = 0; i < result.Referees.Count; i++)
        {
            var r = result.Referees[i];
            CheckQuotes(r.Name, cvText, $"Referees[{i}].Name", errors);
            CheckQuotes(r.Position, cvText, $"Referees[{i}].Position", errors);
            CheckQuotes(r.Organisation, cvText, $"Referees[{i}].Organisation", errors);
            CheckQuotes(r.Phone, cvText, $"Referees[{i}].Phone", errors);
        }

        for (int i = 0; i < result.CareerSynopsis.Count; i++)
        {
            var c = result.CareerSynopsis[i];
            CheckQuotes(c.Title, cvText, $"CareerSynopsis[{i}].Title", errors);
            CheckQuotes(c.Organisation, cvText, $"CareerSynopsis[{i}].Organisation", errors);
            CheckQuotes(c.StartYear, cvText, $"CareerSynopsis[{i}].StartYear", errors);
            CheckQuotes(c.EndYear, cvText, $"CareerSynopsis[{i}].EndYear", errors);
        }

        for (int i = 0; i < result.CommendationsAndAwards.Count; i++)
        {
            var c = result.CommendationsAndAwards[i];
            CheckQuotes(c.Description, cvText, $"CommendationsAndAwards[{i}].Description", errors);
            CheckQuotes(c.Year, cvText, $"CommendationsAndAwards[{i}].Year", errors);
        }

        for (int i = 0; i < result.QualificationsDetailed.Count; i++)
        {
            var q = result.QualificationsDetailed[i];
            CheckQuotes(q.Qualification, cvText, $"QualificationsDetailed[{i}].Qualification", errors);
            CheckQuotes(q.Institution, cvText, $"QualificationsDetailed[{i}].Institution", errors);
            CheckQuotes(q.Year, cvText, $"QualificationsDetailed[{i}].Year", errors);
        }

        for (int i = 0; i < result.TechVotes.Count; i++)
        {
            var vote = result.TechVotes[i];
            if (vote.Keep)
            {
                if (string.IsNullOrWhiteSpace(vote.RfqQuote))
                    errors.Add(new ValidationError($"TechVotes[{i}]", $"'{vote.Technology}' kept but has no RfqQuote."));
                else if (!jobRequirements.Contains(vote.RfqQuote, StringComparison.OrdinalIgnoreCase))
                    errors.Add(new ValidationError($"TechVotes[{i}]", $"RfqQuote for '{vote.Technology}' not found in jobRequirements."));
            }
        }

        return errors;
    }

    private void CheckQuotes(FieldQuotes field, string sourceText, string fieldPath, List<ValidationError> errors)
    {
        foreach (var quote in field.SourceQuotes)
        {
            if (!sourceText.Contains(quote, StringComparison.OrdinalIgnoreCase))
                errors.Add(new ValidationError(fieldPath, $"Quote not found verbatim in source: \"{quote}\""));
        }
    }

    // ValidateDocument(CvDocument, ...) added later, once stage 3 assembles a real CvDocument
}