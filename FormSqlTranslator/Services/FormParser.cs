using System.Text.RegularExpressions;
using System.Xml.Linq;
using FormSqlTranslator.Models;

namespace FormSqlTranslator.Services;

public sealed class FormParser(SqlBlockClassifier classifier)
{
    private static readonly Regex ComponentRegex = new(
        "<component\\s+[^>]*cmptype=\"(?<cmptype>DataSet|SubSelect|Action|ActionRouter)\"[^>]*>(?<body>.*?)</component>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex AttrRegex = new("(?<name>\\w+)=\"(?<value>[^\"]*)\"", RegexOptions.Compiled);
    private static readonly Regex CDataRegex = new("<!\\[CDATA\\[(?<sql>.*?)\\]\\]>", RegexOptions.Singleline | RegexOptions.Compiled);

    public IReadOnlyList<ExtractedSqlBlock> ExtractBlocks(string filePath)
    {
        var text = File.ReadAllText(filePath);
        return TryParseXml(filePath, text, out var xmlBlocks)
            ? xmlBlocks
            : ParseFallback(filePath, text);
    }

    private bool TryParseXml(string filePath, string text, out IReadOnlyList<ExtractedSqlBlock> blocks)
    {
        blocks = [];
        try
        {
            var doc = XDocument.Parse(text, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
            var nodes = doc.Descendants("component")
                .Where(c =>
                {
                    var t = (string?)c.Attribute("cmptype");
                    return t is "DataSet" or "SubSelect" or "Action" or "ActionRouter";
                })
                .ToList();

            var result = new List<ExtractedSqlBlock>();
            var i = 0;
            foreach (var n in nodes)
            {
                var sql = string.Concat(n.Nodes().OfType<XCData>().Select(c => c.Value)).Trim();
                if (string.IsNullOrWhiteSpace(sql))
                    continue;

                var type = (string?)n.Attribute("cmptype") ?? "Unknown";
                result.Add(BuildBlock(filePath, ++i, type, (string?)n.Attribute("name"), (string?)n.Attribute("condition"), sql, n.GetAbsoluteXPath()));
            }

            blocks = result;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private IReadOnlyList<ExtractedSqlBlock> ParseFallback(string filePath, string text)
    {
        var result = new List<ExtractedSqlBlock>();
        var i = 0;

        foreach (Match component in ComponentRegex.Matches(text))
        {
            var header = component.Value[..Math.Min(component.Value.Length, component.Value.IndexOf('>') + 1)];
            var attrs = AttrRegex.Matches(header).ToDictionary(m => m.Groups["name"].Value, m => m.Groups["value"].Value, StringComparer.OrdinalIgnoreCase);
            var body = component.Groups["body"].Value;
            var sql = string.Join("\n", CDataRegex.Matches(body).Select(x => x.Groups["sql"].Value.Trim())).Trim();
            if (string.IsNullOrWhiteSpace(sql))
                continue;

            var type = attrs.GetValueOrDefault("cmptype") ?? "Unknown";
            result.Add(BuildBlock(filePath, ++i, type, attrs.GetValueOrDefault("name"), attrs.GetValueOrDefault("condition"), sql, $"/fallback/component[{i}]"));
        }

        return result;
    }

    private ExtractedSqlBlock BuildBlock(string filePath, int order, string type, string? name, string? condition, string sql, string originPath)
    {
        var blockType = classifier.Classify(sql);
        var isTranslated = condition?.Contains("TYPE_DATABASE=POSTGRE", StringComparison.OrdinalIgnoreCase) == true;
        return new ExtractedSqlBlock(
            BlockId: $"{Path.GetFileName(filePath)}-{order}",
            FilePath: filePath,
            ComponentType: type,
            ComponentName: name,
            Condition: condition,
            Order: order,
            Sql: sql,
            OriginPath: originPath,
            BlockType: blockType,
            IsTranslatedBranch: isTranslated);
    }
}

file static class XElementExtensions
{
    public static string GetAbsoluteXPath(this XElement element)
    {
        var ancestors = element.AncestorsAndSelf().Reverse().Select(x => x.Name.LocalName);
        return "/" + string.Join('/', ancestors);
    }
}
