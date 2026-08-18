// CvPipeline.Tests/NormalisationRulesTests.cs
using CvPipeline.Api.Cv.Application.Normalisation;
using Xunit;

namespace CvPipeline.Tests;

public class NormalisationRulesTests
{
    [Fact] public void N1_Adelaide() =>
        Assert.Equal("Adelaide, SA", NormalisationRules.TryN1_Location("Adelaide SA 5000")!.Value);

    [Fact] public void N1_NoMatch_ReturnsNull() =>
        Assert.Null(NormalisationRules.TryN1_Location("Sydney"));

    [Fact] public void N2_AMonth() =>
        Assert.Equal("4 weeks notice", NormalisationRules.TryN2_Availability("Would need roughly a month to wrap up properly")!.Value);

    [Fact] public void N2_TwoMonths() =>
        Assert.Equal("8 weeks notice", NormalisationRules.TryN2_Availability("About 2 months notice needed")!.Value);

    [Fact] public void N3_BackgroundCheck() =>
        Assert.Equal("National Police Check - Current",
            NormalisationRules.TryN3_BackgroundCheck("Current National Police Check, did it last year for the Northline contract.")!.Value);

    [Fact] public void N5_CTO() =>
        Assert.Equal("Chief Technology Officer", NormalisationRules.TryN5_RefereePosition("CTO")!.Value);

    [Fact] public void N6_Present() =>
        Assert.Equal("Current", NormalisationRules.TryN6_Dates("present")!.Value);

    [Fact] public void N6_Year() =>
        Assert.Equal("2024", NormalisationRules.TryN6_Dates("Feb 2024")!.Value);

    [Fact] public void N7_CKA() =>
        Assert.Equal("Certified Kubernetes Administrator (CKA)",
            NormalisationRules.TryN7_Certification("CKA – Certified Kubernetes Administrator (2022, need to renew this)")!.Value.Name.Value);

    [Fact] public void N8_GraduationYear() =>
        Assert.Equal("2009", NormalisationRules.TryN8_QualificationYear("2006 to 2009, graduated 2009")!.Value);

    [Fact] public void N9_StripParenthetical() =>
        Assert.Equal("2022", NormalisationRules.TryN9_StripEditorialising("(2022, need to renew this)")!.Value);

    [Fact] public void O3_HighSchool_IsPreTertiary() =>
        Assert.True(RelevanceFilter.IsPreTertiary("Adelaide High School, 2004"));

    [Fact] public void O2_BarAttendant_IsUnrelated() =>
        Assert.True(RelevanceFilter.IsUnrelatedRole("Bar attendant"));

    [Fact] public void O2_SoftwareEngineer_IsRelated() =>
        Assert.False(RelevanceFilter.IsUnrelatedRole("Senior Software Engineer"));
}