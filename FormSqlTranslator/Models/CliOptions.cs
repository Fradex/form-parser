namespace FormSqlTranslator.Models;

public sealed record CliOptions(
    string Input,
    string Output,
    string TranslatorUrl,
    string Mode,
    bool Recursive,
    bool DryRun,
    bool SaveIntermediate,
    int MaxDegree)
{
    public static CliOptions Parse(string[] args)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < args.Length; i += 2)
        {
            if (i + 1 >= args.Length || !args[i].StartsWith("--", StringComparison.Ordinal))
                throw new ArgumentException($"Invalid argument near '{args[i]}'");
            map[args[i]] = args[i + 1];
        }

        return new CliOptions(
            Input: map.GetValueOrDefault("--input") ?? throw new ArgumentException("--input is required"),
            Output: map.GetValueOrDefault("--output") ?? "./out",
            TranslatorUrl: map.GetValueOrDefault("--translator-url") ?? "http://192.168.241.141:8081/sql",
            Mode: map.GetValueOrDefault("--mode") ?? "both",
            Recursive: bool.TryParse(map.GetValueOrDefault("--recursive"), out var recursive) ? recursive : true,
            DryRun: bool.TryParse(map.GetValueOrDefault("--dry-run"), out var dryRun) && dryRun,
            SaveIntermediate: !bool.TryParse(map.GetValueOrDefault("--save-intermediate"), out var saveInt) || saveInt,
            MaxDegree: int.TryParse(map.GetValueOrDefault("--max-degree"), out var maxDegree) ? Math.Max(1, maxDegree) : Environment.ProcessorCount);
    }
}
