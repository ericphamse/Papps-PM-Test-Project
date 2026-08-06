using System.Text.RegularExpressions;
using CvPipeline.Api.Cv.Application.Analysis.ExtractCv;

namespace CvPipeline.Api.Cv.Application.Normalisation;

public static class RelevanceFilter
{
    // O2: house policy — job titles judged not relevant to a proposed technical role.
    // This is a documented, testable decision (not model judgment) — extend this list
    // as needed and note the choice in the README.
    private static readonly string[] NonProfessionalTitleKeywords =
    {
        "bar attendant", "waiter", "waitress", "retail assistant", "cashier",
        "cleaner", "labourer", "barista"
    };
    public static bool IsUnrelatedRole(string jobTitle)
        => NonProfessionalTitleKeywords.Any(k => jobTitle.Contains(k, StringComparison.OrdinalIgnoreCase));


    // O3: drop pre-tertiary education from the qualifications list entirely
    public static bool IsPreTertiary(string qualificationText)
        => NormalisationRules.ClassifyTier(qualificationText) == NormalisationRules.QualificationTier.Foundation;

    // O6: apply Gemini's verified tech votes. A "keep" only survives if its rfqQuote
    // is a genuine substring of jobRequirements — same check P3 applies to a derived
    // field. A keep with no quote, or a quote that doesn't verify, is a drop. Without
    // this check the model's vote is trusted unverified, which is exactly the thing
    // stage 1 is supposed to never allow.
    public static List<string> ApplyTechVotes(List<TechVote> votes, string jobRequirements)
        => votes
            .Where(v => v.Keep
                     && !string.IsNullOrWhiteSpace(v.RfqQuote)
                     && jobRequirements.Contains(v.RfqQuote, StringComparison.Ordinal))
            .Select(v => v.Technology)
            .ToList();

    // O5 / O8: commercial terms and offer/condition asides that can ride along inside
    // an otherwise legitimate span (e.g. availability text). Known-phrase coverage,
    // same shape and same caveat as O7 in Rules.cs — extend as real examples turn up,
    // don't chase hypothetical phrasing.
    private static readonly string[] CommercialAndOfferPhrases =
    {
        "commercial-in-confidence", "commercial in confidence",
        "can dig up a third from cartwright if needed",
        "can dig up a third", "rate is",
    };

    public static string StripCommercialAndOfferClauses(string text)
    {
        string result = text;
        foreach (var phrase in CommercialAndOfferPhrases)
            result = Regex.Replace(result, Regex.Escape(phrase), "", RegexOptions.IgnoreCase);

        result = Regex.Replace(result, @",\s*,", ",");
        result = Regex.Replace(result, @"\s{2,}", " ");
        result = Regex.Replace(result, @",\s*$", "");
        return result.Trim();
    }

    // O1: the candidate's own contact details must never ship. Pull every
    // email/phone/github/linkedin-looking string out of the raw CV text, then
    // subtract anything that was legitimately placed into referees[].phone —
    // referee numbers live in the same source text and must be allowed to survive.
    // This is why O1 can't just be "no phone numbers in the output": it has to be
    // "no phone numbers EXCEPT the ones we deliberately kept for referees".
    private static readonly Regex EmailPattern = new(@"[\w.+-]+@[\w-]+\.[\w.-]+", RegexOptions.Compiled);
    private static readonly Regex PhonePattern = new(@"\b0\d{1,3}[\s-]?\d{3}[\s-]?\d{3}\b", RegexOptions.Compiled);
    private static readonly Regex ContactUrlPattern = new(@"\b(?:github\.com|linkedin\.com)/\S+", RegexOptions.Compiled);

    public static List<string> ExtractCandidateContactStrings(string cvText, IEnumerable<string> refereePhones)
    {
        var found = new List<string>();
        found.AddRange(EmailPattern.Matches(cvText).Select(m => m.Value));
        found.AddRange(PhonePattern.Matches(cvText).Select(m => m.Value));
        found.AddRange(ContactUrlPattern.Matches(cvText).Select(m => m.Value.TrimEnd('.', ',')));

        var refereeSet = refereePhones.Where(p => !string.IsNullOrWhiteSpace(p)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return found.Where(s => !refereeSet.Contains(s)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    // Sweep: does the assembled output contain any candidate contact string?
    // Call this after stage 3 (over the model's assembled document, Analysis) and
    // again at POST /api/generations (over the user-confirmed document, Gate 2).
    // Two different untrusted parties, same check — neither call site is redundant.
    public static List<string> FindPiiLeaks(string assembledOutputText, IEnumerable<string> candidateContactStrings)
        => candidateContactStrings
            .Where(s => assembledOutputText.Contains(s, StringComparison.OrdinalIgnoreCase))
            .ToList();
}