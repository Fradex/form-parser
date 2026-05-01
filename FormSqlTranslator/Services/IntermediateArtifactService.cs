using FormSqlTranslator.Models;

namespace FormSqlTranslator.Services;

public sealed class IntermediateArtifactService
{
    public async Task SaveOriginalAsync(string outputRoot, string filePath, string content, CancellationToken ct)
    {
        var dir = GetFileDir(outputRoot, filePath);
        Directory.CreateDirectory(dir);
        await File.WriteAllTextAsync(Path.Combine(dir, "00-original.txt"), content, ct);
    }

    public async Task SaveBlockAsync(string outputRoot, ExtractedSqlBlock block, string stage, string extension, string content, CancellationToken ct)
    {
        var dir = Path.Combine(GetFileDir(outputRoot, block.FilePath), stage);
        Directory.CreateDirectory(dir);
        await File.WriteAllTextAsync(Path.Combine(dir, $"{block.SafeFileId}.{extension}"), content, ct);
    }

    private static string GetFileDir(string outputRoot, string filePath)
    {
        var fileId = filePath.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Replace(':', '_')
            .Replace(Path.DirectorySeparatorChar, '_')
            .Replace(Path.AltDirectorySeparatorChar, '_');
        return Path.Combine(outputRoot, "intermediate", fileId);
    }
}
