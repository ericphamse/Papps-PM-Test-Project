using DocumentFormat.OpenXml.Packaging;

namespace CvPipeline.Api.Cv.Infrastructure.Documents;

public interface ICvTextExtractor
{
    Task<string> ExtractTextAsync(IFormFile file, CancellationToken ct);
}

public class StubCvTextExtractor : ICvTextExtractor
{
    public async Task<string> ExtractTextAsync(IFormFile file, CancellationToken ct)
    {
        if (Path.GetExtension(file.FileName).Equals(".docx", StringComparison.OrdinalIgnoreCase))
            return ExtractDocxText(file);

        using var reader = new StreamReader(file.OpenReadStream());
        return await reader.ReadToEndAsync();
    }

    private static string ExtractDocxText(IFormFile file)
    {
        using var stream = file.OpenReadStream();
        using var wordDoc = WordprocessingDocument.Open(stream, false);
        return wordDoc.MainDocumentPart?.Document?.Body?.InnerText ?? "";
    }
}