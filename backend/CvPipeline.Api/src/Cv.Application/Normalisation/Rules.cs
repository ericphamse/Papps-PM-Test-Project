using System.Text.RegularExpressions;
using CvPipeline.Api.Cv.Domain;

namespace CvPipeline.Api.Cv.Application.Normalisation;

public record RuleResult(string Value, string RuleId);

public static class NormalisationRules
{
    public static RuleResult? TryN1_Location(string span)
    {
        var m = Regex.Match(span.Trim(), @"^(?<suburb>.+?)\s+(?<state>[A-Z]{2,3})\s+\d{4}$");
        return m.Success ? new($"{m.Groups["suburb"].Value}, {m.Groups["state"].Value}", "N1") : null;
    }

    private static readonly Dictionary<string, double> DurationUnitToWeeks = new()
    {
        ["day"] = 1.0 / 7, ["days"] = 1.0 / 7,
        ["week"] = 1, ["weeks"] = 1,
        ["month"] = 4, ["months"] = 4,
    };

    private static readonly Dictionary<string, int> WordNumbers = new()
    {
        ["a"] = 1, ["an"] = 1, ["one"] = 1, ["two"] = 2, ["three"] = 3, ["four"] = 4,
        ["couple"] = 2, ["few"] = 3,
    };

    private static readonly Dictionary<string, string> ImmediatePhrases = new()
    {
        ["immediate"] = "Immediate", ["immediately"] = "Immediate",
        ["asap"] = "Immediate", ["straight away"] = "Immediate",
        ["available now"] = "Immediate", ["no notice"] = "Immediate",
    };

    public static RuleResult? TryN2_Availability(string span)
    {
        string lower = span.ToLowerInvariant();

        var digit = Regex.Match(lower, @"(\d+)\s*(day|days|week|weeks|month|months)\b");
        if (digit.Success && DurationUnitToWeeks.TryGetValue(digit.Groups[2].Value, out var factor))
            return new($"{(int)Math.Round(int.Parse(digit.Groups[1].Value) * factor)} weeks notice", "N2");

        var worded = Regex.Match(lower, @"\b(a|an|one|two|three|four|couple(?:\s+of)?|few)\b\s+(day|days|week|weeks|month|months)\b");
        if (worded.Success)
        {
            var numberKey = worded.Groups[1].Value.Replace(" of", "");
            if (WordNumbers.TryGetValue(numberKey, out var n) && DurationUnitToWeeks.TryGetValue(worded.Groups[2].Value, out var unitFactor))
                return new($"{(int)Math.Round(n * unitFactor)} weeks notice", "N2");
        }

        foreach (var (phrase, result) in ImmediatePhrases)
            if (lower.Contains(phrase)) return new(result, "N2");

        return null;
    }

    public static RuleResult? TryN3_BackgroundCheck(string span)
    {
        var m = Regex.Match(span, @"(?:Current\s+)?(?<check>[A-Z][a-zA-Z ]*?Check)");
        return m.Success ? new($"{m.Groups["check"].Value.Trim()} - Current", "N3") : null;
    }

    private static readonly Dictionary<string, string> TitleExpansions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Intern"] = "Software Development Intern",
    };
    public static RuleResult? TryN4_JobTitle(string span)
    {
        string trimmed = span.Trim();
        return TitleExpansions.TryGetValue(trimmed, out var expanded) ? new(expanded, "N4") : null;
    }

    private static readonly Dictionary<string, string> AcronymExpansions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["CTO"] = "Chief Technology Officer",
        ["CEO"] = "Chief Executive Officer",
        ["CFO"] = "Chief Financial Officer",
        ["COO"] = "Chief Operating Officer",
    };
    public static RuleResult? TryN5_RefereePosition(string span)
    {
        var m = Regex.Match(span, @"\b(CTO|CEO|CFO|COO)\b", RegexOptions.IgnoreCase);
        return m.Success && AcronymExpansions.TryGetValue(m.Value, out var full) ? new(full, "N5") : null;
    }

    public static RuleResult? TryN6_Dates(string span)
    {
        string trimmed = span.Trim();
        if (trimmed.Equals("present", StringComparison.OrdinalIgnoreCase)) return new("Current", "N6");

        var m4 = Regex.Match(trimmed, @"(?<year>\d{4})");
        if (m4.Success) return new(m4.Groups["year"].Value, "N6");

        var m2 = Regex.Match(trimmed, @"(?<year>\d{2})$");
        if (m2.Success)
        {
            int yy = int.Parse(m2.Groups["year"].Value);
            int fullYear = yy <= 30 ? 2000 + yy : 1900 + yy;
            return new(fullYear.ToString(), "N6");
        }
        return null;
    }

    public static (RuleResult Name, string? Year)? TryN7_Certification(string span)
    {
        var m = Regex.Match(span, @"^(?<acronym>[A-Z]{2,6})\s*[-–—]\s*(?<full>[^(]+?)\s*(\((?<year>\d{4})[^)]*\))?$");
        if (!m.Success || !m.Groups["full"].Success) return null;
        string name = $"{m.Groups["full"].Value.Trim()} ({m.Groups["acronym"].Value})";
        string? year = m.Groups["year"].Success ? m.Groups["year"].Value : null;
        return (new RuleResult(name, "N7"), year);
    }

    public static RuleResult? TryN8_QualificationYear(string span)
    {
        var m = Regex.Match(span, @"(graduated|finished|awarded)\s+(?<year>\d{4})", RegexOptions.IgnoreCase);
        return m.Success ? new(m.Groups["year"].Value, "N8") : null;
    }

    private static readonly string[] AsideStarters =
    {
        "did it", "need to", "which i", "hasn't", "just give", "probably",
        "honestly", "genuinely", "still think"
    };

    public static RuleResult? TryN9_StripEditorialising(string span)
    {
        var paren = Regex.Match(span, @"\((?<year>\d{4}),\s*[^)]+\)");
        if (paren.Success) return new(paren.Groups["year"].Value, "N9");

        var parts = span.Split(',');
        if (parts.Length > 1)
        {
            var kept = parts.Where(p => !AsideStarters.Any(a => p.Trim().StartsWith(a, StringComparison.OrdinalIgnoreCase))).ToList();
            if (kept.Count < parts.Length)
                return new(string.Join(",", kept).Trim(), "N9");
        }
        return null;
    }


    public record StudyPeriod(string Title, int StartYear, int EndYear);

    public static StudyPeriod? TryN10_StudyPeriod(string qualificationSpan)
    {
        if (qualificationSpan.Contains("part time", StringComparison.OrdinalIgnoreCase)) return null;

        var m = Regex.Match(qualificationSpan,
            @"^(?<degree>[^,]+),\s*(?<inst>[^—-]+)[—-]\s*(?<start>\d{4})\s*to\s*(?<end>\d{4})");
        if (!m.Success) return null;

        return new StudyPeriod(
            $"{m.Groups["degree"].Value.Trim()} (full-time study)",
            int.Parse(m.Groups["start"].Value),
            int.Parse(m.Groups["end"].Value));
    }

    public enum QualificationTier { Postgraduate = 1, Undergraduate = 2, Certification = 3, Foundation = 4 }

    public static QualificationTier ClassifyTier(string qualificationText)
    {
        string lower = qualificationText.ToLowerInvariant();
        if (lower.Contains("master") || lower.Contains("phd") || lower.Contains("doctorate")) return QualificationTier.Postgraduate;
        if (lower.Contains("bachelor")) return QualificationTier.Undergraduate;
        if (lower.Contains("high school") || lower.Contains("secondary")) return QualificationTier.Foundation;
        return QualificationTier.Certification;
    }

    public static List<Referee> SelectReferees(
        List<Referee> candidates, List<CareerEntry> careerSynopsis, int currentYear, int recentYears = 3)
    {
        var recentOrganisations = careerSynopsis
            .Where(c => !string.IsNullOrWhiteSpace(c.Organisation) &&
                        (c.EndYear.Equals("Current", StringComparison.OrdinalIgnoreCase) ||
                         (int.TryParse(c.EndYear, out var endYear) && currentYear - endYear <= recentYears)))
            .Select(c => c.Organisation)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return candidates
            .OrderByDescending(r => recentOrganisations.Contains(r.Organisation) ? 1 : 0)
            .Take(2)
            .ToList();
    }

    private static readonly string[] HedgingPhrases =
    {
        "which i still think was mostly luck",
        "hasn't fallen over yet",
        "need to renew this",
        "just give me a heads up first",
        "which i still think",
        "mostly luck",
        "probably not relevant",
        "not exaggerating",
        "genuinely",
        "honestly"
    };

    public static string ApplyO7_StripHedging(string text)
    {
        string result = text;
        foreach (var phrase in HedgingPhrases)
        {
            result = Regex.Replace(result, Regex.Escape(phrase), "", RegexOptions.IgnoreCase);
        }
        result = Regex.Replace(result, @",\s*,", ",");
        result = Regex.Replace(result, @"\s{2,}", " ");
        result = Regex.Replace(result, @",\s*$", "");
        return result.Trim();
    }
}