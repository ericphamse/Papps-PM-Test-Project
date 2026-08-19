namespace CvPipeline.Api.Cv.Infrastructure.Documents;

public interface ICvTextExtractor
{
    Task<string> ExtractTextAsync(IFormFile file, CancellationToken ct);
}

public class StubCvTextExtractor : ICvTextExtractor
{
    public Task<string> ExtractTextAsync(IFormFile file, CancellationToken ct)
    {
        using var reader = new StreamReader(file.OpenReadStream());
        return reader.ReadToEndAsync();
    }
}