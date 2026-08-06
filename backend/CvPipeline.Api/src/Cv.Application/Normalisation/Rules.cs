// src/Cv.Application/Normalisation/NormalisationRules.cs
using System.Text.RegularExpressions;
using CvPipeline.Api.Cv.Domain;

namespace CvPipeline.Api.Cv.Application.Normalisation;

public record RuleResult(string Value, string RuleId);

public static class NormalisationRules
{
    // N1: "Adelaide SA 5000" -> "Adelaide, SA"
    public static RuleResult? TryN1_Location(string span)
    {
        var m = Regex.Match(span.Trim(), @"^(?<suburb>.+?)\s+(?<state>[A-Z]{2,3})\s+\d{4}$");
        return m.Success ? new($"{m.Groups["suburb"].Value}, {m.Groups["state"].Value}", "N1") : null;
    }

    // N2: convert a notice period to weeks. Covers explicit numeric durations
    // (days/weeks/months), worded numbers ("a month", "a couple of weeks"), and
    // common immediate-start idioms. House policy: 1 month = 4 weeks — a decision,
    // not a fact, same as N11's tie-break; document it in the README. Anything
    // outside this coverage (a date-anchored phrase, an unrecognised idiom) falls
    // through to null: the caller keeps the verified span and warns rather than
    // guessing a number (P1's "reclassify or warn, never invent").
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

        // "<number> <unit>" — e.g. "2 months", "10 days"
        var digit = Regex.Match(lower, @"(\d+)\s*(day|days|week|weeks|month|months)\b");
        if (digit.Success && DurationUnitToWeeks.TryGetValue(digit.Groups[2].Value, out var factor))
            return new($"{(int)Math.Round(int.Parse(digit.Groups[1].Value) * factor)} weeks notice", "N2");

        // "<worded number> <unit>" — e.g. "a month", "a couple of weeks"
        var worded = Regex.Match(lower, @"\b(a|an|one|two|three|four|couple(?:\s+of)?|few)\b\s+(day|days|week|weeks|month|months)\b");
        if (worded.Success)
        {
            var numberKey = worded.Groups[1].Value.Replace(" of", "");
            if (WordNumbers.TryGetValue(numberKey, out var n) && DurationUnitToWeeks.TryGetValue(worded.Groups[2].Value, out var unitFactor))
                return new($"{(int)Math.Round(n * unitFactor)} weeks notice", "N2");
        }

        // known immediate-start idioms
        foreach (var (phrase, result) in ImmediatePhrases)
            if (lower.Contains(phrase)) return new(result, "N2");

        return null;
    }

    // N3: "Current National Police Check, did it last year..." -> "National Police Check - Current"
    public static RuleResult? TryN3_BackgroundCheck(string span)
    {
        var m = Regex.Match(span, @"(?:Current\s+)?(?<check>[A-Z][a-zA-Z ]*?Check)");
        return m.Success ? new($"{m.Groups["check"].Value.Trim()} - Current", "N3") : null;
    }

    // N4: "Intern" -> "Software Development Intern"
    private static readonly Dictionary<string, string> TitleExpansions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Intern"] = "Software Development Intern",
    };
    public static RuleResult? TryN4_JobTitle(string span)
    {
        string trimmed = span.Trim();
        return TitleExpansions.TryGetValue(trimmed, out var expanded) ? new(expanded, "N4") : null;
    }

    // N5: "CTO" -> "Chief Technology Officer"
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

    // N6: "Feb 2024 - present" fragment -> year only, "present" -> "Current"
    public static RuleResult? TryN6_Dates(string span)
    {
        string trimmed = span.Trim();
        if (trimmed.Equals("present", StringComparison.OrdinalIgnoreCase)) return new("Current", "N6");

        var m4 = Regex.Match(trimmed, @"(?<year>\d{4})");
        if (m4.Success) return new(m4.Groups["year"].Value, "N6");

        // 2-digit year fallback, e.g. "Jan 09" -> 2009, "Oct 10" -> 2010
        var m2 = Regex.Match(trimmed, @"(?<year>\d{2})$");
        if (m2.Success)
        {
            int yy = int.Parse(m2.Groups["year"].Value);
            int fullYear = yy <= 30 ? 2000 + yy : 1900 + yy; // reasonable pivot for CV dates
            return new(fullYear.ToString(), "N6");
        }
        return null;
    }

    // N7: "CKA – Certified Kubernetes Administrator (2022, ...)"
    //     -> name "Certified Kubernetes Administrator (CKA)", year "2022"
    public static (RuleResult Name, string? Year)? TryN7_Certification(string span)
    {
        var m = Regex.Match(span, @"^(?<acronym>[A-Z]{2,6})\s*[-–—]\s*(?<full>[^(]+?)\s*(\((?<year>\d{4})[^)]*\))?$");
        if (!m.Success || !m.Groups["full"].Success) return null;
        string name = $"{m.Groups["full"].Value.Trim()} ({m.Groups["acronym"].Value})";
        string? year = m.Groups["year"].Success ? m.Groups["year"].Value : null;
        return (new RuleResult(name, "N7"), year);
    }

    // N8: "2006 to 2009, graduated 2009" -> "2009"
    public static RuleResult? TryN8_QualificationYear(string span)
    {
        var m = Regex.Match(span, @"(graduated|finished|awarded)\s+(?<year>\d{4})", RegexOptions.IgnoreCase);
        return m.Success ? new(m.Groups["year"].Value, "N8") : null;
    }

    // N9: "(2022, need to renew this)" -> "2022"
    // N9: strip trailing editorializing clauses, e.g.
    // "...finished 2016, did it part time while working" -> "...finished 2016"
    private static readonly string[] AsideStarters =
    {
        "did it", "need to", "which i", "hasn't", "just give", "probably",
        "honestly", "genuinely", "still think"
    };

    public static RuleResult? TryN9_StripEditorialising(string span)
    {
        // Original narrow case: "(2022, need to renew this)" -> "2022"
        var paren = Regex.Match(span, @"\((?<year>\d{4}),\s*[^)]+\)");
        if (paren.Success) return new(paren.Groups["year"].Value, "N9");

        // General case: strip a trailing comma-clause that starts with a known aside phrase
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

    //N10: Detects "<Degree>, <Institution> — YYYY to YYYY" WITHOUT "part time" mentioned nearby
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

    // N11: seniority tiers, used to rank qualifications for the details-table top-4
    public enum QualificationTier { Postgraduate = 1, Undergraduate = 2, Certification = 3, Foundation = 4 }

    public static QualificationTier ClassifyTier(string qualificationText)
    {
        string lower = qualificationText.ToLowerInvariant();
        if (lower.Contains("master") || lower.Contains("phd") || lower.Contains("doctorate")) return QualificationTier.Postgraduate;
        if (lower.Contains("bachelor")) return QualificationTier.Undergraduate;
        if (lower.Contains("high school") || lower.Contains("secondary")) return QualificationTier.Foundation;
        return QualificationTier.Certification;
    }

    // S5: exactly 2 referees, at least one a direct supervisor from the last N years.
    // "direct supervisor" is inferred structurally, not semantically: a referee whose
    // organisation matches a career-synopsis entry that is Current (or ended within
    // the last N years) outranks one who doesn't. This is a selection rule, not a
    // text rewrite — like N11, it decides which candidates survive, not what they say.
    // If the CV's referee data never gives you an organisation, this can't do better
    // than "keep the first two" — say so in the README rather than guessing further.
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

    // O7: strip hedging, self-deprecation, and asides from any text span
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
        // Clean up leftover double commas/spaces from removed clauses
        result = Regex.Replace(result, @",\s*,", ",");
        result = Regex.Replace(result, @"\s{2,}", " ");
        result = Regex.Replace(result, @",\s*$", "");
        return result.Trim();
    }
}