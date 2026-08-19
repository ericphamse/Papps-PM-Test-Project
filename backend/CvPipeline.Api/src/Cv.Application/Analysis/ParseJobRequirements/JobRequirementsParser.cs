using System.Text.RegularExpressions;
using CvPipeline.Api.Cv.Domain;

namespace CvPipeline.Api.Cv.Application.Analysis.ParseJobRequirements;

public record ParsedJobRequirements(
    FieldValue<string> RoleTitle,
    FieldValue<string> Level,
    FieldValue<string> ProposedRole
);

public static class JobRequirementsParser
{
    public static ParsedJobRequirements Parse(string jobRequirementsText)
    {
        return new ParsedJobRequirements(
            RoleTitle: ExtractTableField(jobRequirementsText, "Engagement title", "J1"),
            Level: ExtractTableField(jobRequirementsText, "Level required", "J2"),
            ProposedRole: ExtractTableField(jobRequirementsText, "Panel category", "J3")
        );
    }

    private static FieldValue<string> ExtractTableField(string text, string label, string ruleId)
    {
        var match = Regex.Match(
            text,
            $@"{Regex.Escape(label)}\s*[|:]\s*(?<value>[^\r\n|]+)",
            RegexOptions.IgnoreCase);

        if (!match.Success)
            return new FieldValue<string>(null, Provenance.Absent, Array.Empty<string>());

        string value = match.Groups["value"].Value.Trim();
        string quote = match.Value.Trim();

        return new FieldValue<string>(value, Provenance.Derived, new[] { quote }, new[] { ruleId });
    }
}