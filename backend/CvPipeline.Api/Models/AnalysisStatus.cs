namespace CvPipeline.Api.Models;

public static class AnalysisStatus
{
    public const string Extracting = "extracting";
    public const string Tailoring = "tailoring";
    public const string Review = "review";
    public const string Generated = "generated";
    public const string Failed = "failed";
}