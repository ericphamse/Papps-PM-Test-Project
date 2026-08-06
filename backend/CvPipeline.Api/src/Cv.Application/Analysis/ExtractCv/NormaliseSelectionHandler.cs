// src/Cv.Application/Analysis/ExtractCv/NormaliseSelectionHandler.cs
using CvPipeline.Api.Cv.Domain;
using CvPipeline.Api.Cv.Application.Normalisation;
using CvPipeline.Api.Cv.Application.Analysis.ParseJobRequirements;

namespace CvPipeline.Api.Cv.Application.Analysis.ExtractCv;

public class NormaliseSelectionHandler
{
    public CvDocument Normalise(SelectionResult selection, ParsedJobRequirements parsedJd, string cvText, string jobRequirements)
    {
        var fullName = NormaliseVerbatim(selection.FullName);

        var securityClearance = NormaliseScalar(selection.SecurityClearance, cvText, NormalisationRules.TryN3_BackgroundCheck);
        var availability = NormaliseScalar(selection.Availability, cvText, NormalisationRules.TryN2_Availability);
        var location = NormaliseScalar(selection.Location, cvText, NormalisationRules.TryN1_Location);
        var careerSynopsis = NormaliseCareerSynopsis(selection.CareerSynopsis, selection.Qualifications);
        var referees = NormaliseReferees(selection.Referees, careerSynopsis.Value ?? new List<CareerEntry>());   // S5 needs careerSynopsis, so it's built first
        var commendations = NormaliseCommendations(selection.CommendationsAndAwards);
        var qualificationsDetailed = NormaliseQualificationsDetailed(selection.QualificationsDetailed);
        var qualifications = BuildQualificationsSummary(qualificationsDetailed.Value ?? new List<QualificationEntry>());

        // O6 result feeds stage 3's prompt later — not a CvDocument field itself.
        // ApplyTechVotes verifies every rfqQuote against jobRequirements before
        // honouring a "keep" (P3-style check) — a vote with no verified quote is a drop.
        var keptTechnologies = RelevanceFilter.ApplyTechVotes(selection.TechVotes, jobRequirements);

        var absent = new FieldValue<string>(null, Provenance.Absent, Array.Empty<string>());
        var absentList = new FieldValue<List<Competency>>(null, Provenance.Absent, Array.Empty<string>());
        var absentHighlights = new FieldValue<List<Highlight>>(null, Provenance.Absent, Array.Empty<string>());

        return new CvDocument(
            SchemaVersion: 1,
            RoleTitle: parsedJd.RoleTitle,
            Level: parsedJd.Level,
            ProposedRole: parsedJd.ProposedRole,
            FullName: fullName,
            Qualifications: qualifications,
            SecurityClearance: securityClearance,
            YearsOfExperience: absent,           // stage 3 — not built yet
            Availability: availability,
            Location: location,
            Referees: referees,
            CareerSynopsis: careerSynopsis,
            ProfessionalProfile: absent,          // stage 3 — not built yet
            RoleSuitability: absent,              // stage 3 — not built yet
            CoreCompetencies: absentList,         // stage 3 — not built yet
            CommendationsAndAwards: commendations,
            QualificationsDetailed: qualificationsDetailed,
            CareerHighlights: absentHighlights    // stage 3 — not built yet
        );
    }

    private FieldValue<string> NormaliseVerbatim(FieldQuotes quotes)
    {
        if (quotes.SourceQuotes.Length == 0)
            return new FieldValue<string>(null, Provenance.Absent, Array.Empty<string>());
        return new FieldValue<string>(quotes.SourceQuotes[0], Provenance.Verbatim, quotes.SourceQuotes);
    }

    private FieldValue<string> NormaliseScalar(FieldQuotes quotes, string cvText, Func<string, RuleResult?> rule)
    {
        if (quotes.SourceQuotes.Length == 0)
            return new FieldValue<string>(null, Provenance.Absent, Array.Empty<string>());

        string span = RelevanceFilter.StripCommercialAndOfferClauses(quotes.SourceQuotes[0]);                     // ← O5 / O8: strip commercial terms / offer asides that rode along in the same span
        var result = rule(span);
        if (result is not null)
            return new FieldValue<string>(result.Value, Provenance.Normalised, quotes.SourceQuotes, new[] { result.RuleId, "O7" });

        // P1: no rule matched — accept the raw span as verbatim if it's genuinely in the source
        return cvText.Contains(span, StringComparison.OrdinalIgnoreCase)
            ? new FieldValue<string>(span, Provenance.Verbatim, quotes.SourceQuotes)
            : new FieldValue<string>(span, Provenance.Absent, quotes.SourceQuotes);
    }

    private FieldValue<List<string>> NormaliseQualificationsList(FieldQuotes quotes)
    {
        if (quotes.SourceQuotes.Length == 0)
            return new FieldValue<List<string>>(null, Provenance.Absent, Array.Empty<string>());

        var values = new List<string>();
        var ruleIds = new HashSet<string>();

        foreach (var span in quotes.SourceQuotes)
        {
            var n7 = NormalisationRules.TryN7_Certification(span);
            if (n7 is not null) { values.Add(n7.Value.Name.Value); ruleIds.Add("N7"); }
            else values.Add(span);
        }

        return new FieldValue<List<string>>(values, Provenance.Normalised, quotes.SourceQuotes, ruleIds.ToArray());
    }

    private FieldValue<List<Referee>> NormaliseReferees(List<ReferQuotes> referees, List<CareerEntry> careerSynopsis)
    {
        var candidates = new List<Referee>();
        foreach (var r in referees)
        {
            if (r.Name.SourceQuotes.Length == 0) continue;
            string position = r.Position.SourceQuotes.FirstOrDefault() ?? "";
            var n5 = NormalisationRules.TryN5_RefereePosition(position);
            candidates.Add(new Referee(
                r.Name.SourceQuotes[0],
                n5?.Value ?? position,
                r.Organisation.SourceQuotes.FirstOrDefault() ?? "",
                r.Phone.SourceQuotes.FirstOrDefault() ?? ""));
        }

        // S5: exactly 2 referees, at least one a recent/current direct supervisor.
        // TODO: DateTime.UtcNow.Year makes this one call impure — inject the "as of"
        // year the same way you do for any other date-anchored rule, for the same
        // determinism reason (Inv-3).
        var selected = NormalisationRules.SelectReferees(candidates, careerSynopsis, DateTime.UtcNow.Year);

        return new FieldValue<List<Referee>>(selected, Provenance.Normalised, Array.Empty<string>(), new[] { "N5", "S5" });
    }

    private FieldValue<List<CareerEntry>> NormaliseCareerSynopsis(
    List<CareerEntryQuotes> entries, FieldQuotes qualificationsQuotes)   // ← new param
    {
        var results = new List<CareerEntry>();
        foreach (var e in entries)
        {
            if (e.Title.SourceQuotes.Length == 0) continue;
            string title = e.Title.SourceQuotes[0];
            if (RelevanceFilter.IsUnrelatedRole(title)) continue; // O2

            var n4 = NormalisationRules.TryN4_JobTitle(title);
            if (n4 is not null) title = n4.Value;

            var n6Start = NormalisationRules.TryN6_Dates(e.StartYear.SourceQuotes.FirstOrDefault() ?? "");
            var n6End = NormalisationRules.TryN6_Dates(e.EndYear.SourceQuotes.FirstOrDefault() ?? "");

            results.Add(new CareerEntry(title, e.Organisation.SourceQuotes.FirstOrDefault() ?? "",
                n6Start?.Value ?? "", n6End?.Value ?? ""));
        }

        // N10: add a career row for any full-time study period found
        foreach (var qualSpan in qualificationsQuotes.SourceQuotes)
        {
            var study = NormalisationRules.TryN10_StudyPeriod(qualSpan);
            if (study is not null)
                results.Add(new CareerEntry(study.Title, "", study.StartYear.ToString(), study.EndYear.ToString()));
        }

        results = results.OrderByDescending(r => int.TryParse(r.StartYear, out var y) ? y : 0).ToList();

        return new FieldValue<List<CareerEntry>>(results, Provenance.Normalised, Array.Empty<string>(), new[] { "N4", "N6", "N10", "O2" });
    }

    private FieldValue<List<QualificationEntry>> NormaliseQualificationsDetailed(List<QualificationEntryQuotes> entries)
    {
        var results = new List<QualificationEntry>();
        foreach (var e in entries)
        {
            if (e.Qualification.SourceQuotes.Length == 0) continue;
            string qual = e.Qualification.SourceQuotes[0];

            if (RelevanceFilter.IsPreTertiary(qual)) continue; // O3

            string? year = e.Year.SourceQuotes.FirstOrDefault();
            var n7 = NormalisationRules.TryN7_Certification(qual);
            if (n7 is not null) { qual = n7.Value.Name.Value; year = n7.Value.Year ?? year; }

            if (year is not null)
            {
                var n8 = NormalisationRules.TryN8_QualificationYear(year);
                var n9 = NormalisationRules.TryN9_StripEditorialising(year);
                year = n8?.Value ?? n9?.Value ?? System.Text.RegularExpressions.Regex.Match(year, @"\d{4}").Value;
            }

            results.Add(new QualificationEntry(qual, e.Institution.SourceQuotes.FirstOrDefault(), year ?? ""));
        }
        return new FieldValue<List<QualificationEntry>>(results, Provenance.Normalised, Array.Empty<string>(), new[] { "N7", "N8", "N9", "O3" });
    }

    private FieldValue<List<string>> BuildQualificationsSummary(List<QualificationEntry> detailed)
    {
        if (detailed.Count == 0)
            return new FieldValue<List<string>>(null, Provenance.Absent, Array.Empty<string>());

        var ranked = detailed
            .OrderBy(q => NormalisationRules.ClassifyTier(q.Qualification))
            .ThenByDescending(q => int.TryParse(q.Year, out var y) ? y : 0)
            .Take(4)
            .Select(q => q.Qualification)
            .ToList();

        return new FieldValue<List<string>>(ranked, Provenance.Normalised, Array.Empty<string>(), new[] { "N11" });
    }

    private FieldValue<List<DatedEntry>> NormaliseCommendations(List<DatedEntryQuotes> entries)
    {
        var results = entries
            .Where(e => e.Description.SourceQuotes.Length > 0)
            .Select(e => new DatedEntry(
                e.Description.SourceQuotes[0],
                e.Year.SourceQuotes.FirstOrDefault() ?? "Various"))
            .ToList();
        return new FieldValue<List<DatedEntry>>(results, Provenance.Normalised, Array.Empty<string>());
    }
}