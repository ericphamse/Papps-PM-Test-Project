using System.Text.RegularExpressions;
using CvPipeline.Api.Cv.Application.Analysis.ExtractCv;

namespace CvPipeline.Api.Cv.Application.Normalisation;

public static class RelevanceFilter
{
    private static readonly string[] NonProfessionalTitleKeywords =
    {
        "bar attendant", "waiter", "waitress", "retail assistant", "cashier",
        "cleaner", "labourer", "barista"
    };
    public static bool IsUnrelatedRole(string jobTitle)
        => NonProfessionalTitleKeywords.Any(k => jobTitle.Contains(k, StringComparison.OrdinalIgnoreCase));

    public static bool IsPreTertiary(string qualificationText)
        => NormalisationRules.ClassifyTier(qualificationText) == NormalisationRules.QualificationTier.Foundation;

    public static List<string> ApplyTechVotes(List<TechVote> votes, string jobRequirements)
        => votes
            .Where(v => v.Keep
                     && !string.IsNullOrWhiteSpace(v.RfqQuote)
                     && jobRequirements.Contains(v.RfqQuote, StringComparison.Ordinal))
            .Select(v => v.Technology)
            .ToList();

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

    public static List<string> FindPiiLeaks(string assembledOutputText, IEnumerable<string> candidateContactStrings)
        => candidateContactStrings
            .Where(s => assembledOutputText.Contains(s, StringComparison.OrdinalIgnoreCase))
            .ToList();
}