namespace FormSqlTranslator.Services;

public sealed class FileDiscoveryService
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".frm", ".php", ".xml", ".inc"
    };

    public IReadOnlyList<string> Discover(string input, bool recursive)
    {
        if (File.Exists(input))
        {
            return AllowedExtensions.Contains(Path.GetExtension(input))
                ? [Path.GetFullPath(input)]
                : [];
        }

        if (!Directory.Exists(input))
            return [];

        var option = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        return Directory.EnumerateFiles(input, "*.*", option)
            .Where(x => AllowedExtensions.Contains(Path.GetExtension(x)))
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
