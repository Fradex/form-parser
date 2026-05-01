using System.Text.RegularExpressions;
using System.Xml.Linq;
using FormSqlTranslator.Models;

namespace FormSqlTranslator.Services;

public sealed class FormParser(SqlBlockClassifier classifier)
{
    private static readonly Regex ComponentRegex = new(
        "<(component|cmpAction|cmpDataSet)\\s+[^>]*(cmptype=\"(?<cmptype>DataSet|SubSelect|Action|ActionRouter|Script)\")?[^>]*>(?<body>.*?)</(component|cmpAction|cmpDataSet)>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex AttrRegex = new("(?<name>\\w+)=\"(?<value>[^\"]*)\"", RegexOptions.Compiled);
    private static readonly Regex CDataRegex = new("<!\\[CDATA\\[(?<sql>.*?)\\]\\]>", RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex ScriptSqlRegex = new(@"(?<sql>^\s*(select|insert|update|delete|begin|declare)\b[\s\S]*?;)", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);
    private static readonly Regex ScriptCallRegex = new(@"(?<sql>[A-Z][A-Z0-9_\.]*\s*\([^\)]*:[A-Z0-9_]+[^\)]*\))", RegexOptions.IgnoreCase | RegexOptions.Compiled);

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
            var nodes = doc.Descendants()
                .Where(IsSupportedSqlNode)
                .ToList();

            var result = new List<ExtractedSqlBlock>();
            var i = 0;
            foreach (var n in nodes)
            {
                var type = ResolveComponentType(n);
                var sqlText = string.Concat(n.Nodes().OfType<XCData>().Select(c => c.Value)).Trim();
                if (string.IsNullOrWhiteSpace(sqlText) && type.Equals("Action", StringComparison.OrdinalIgnoreCase))
                {
                    sqlText = ExtractActionInlineSql(n);
                }

                if (string.IsNullOrWhiteSpace(sqlText))
                    continue;

                if (type.Equals("Script", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var scriptSql in ExtractFromScript(sqlText))
                    {
                        result.Add(BuildBlock(filePath, ++i, type, (string?)n.Attribute("name"), (string?)n.Attribute("condition"), scriptSql, n.GetAbsoluteXPath()));
                    }
                    continue;
                }

                result.Add(BuildBlock(filePath, ++i, type, (string?)n.Attribute("name"), (string?)n.Attribute("condition"), sqlText, n.GetAbsoluteXPath()));
            }

            blocks = result;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsSupportedSqlNode(XElement element)
    {
        var name = element.Name.LocalName;
        if (name.Equals("component", StringComparison.OrdinalIgnoreCase))
        {
            var t = (string?)element.Attribute("cmptype");
            return t is "DataSet" or "SubSelect" or "Action" or "ActionRouter" or "Script";
        }

        return name is "cmpAction" or "cmpDataSet";
    }

    private static string ResolveComponentType(XElement element)
    {
        var name = element.Name.LocalName;
        if (name.Equals("cmpAction", StringComparison.OrdinalIgnoreCase)) return "Action";
        if (name.Equals("cmpDataSet", StringComparison.OrdinalIgnoreCase)) return "DataSet";
        return (string?)element.Attribute("cmptype") ?? "Unknown";
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
            var tagName = Regex.Match(header, @"<\s*(\w+)").Groups[1].Value;
            var type = attrs.GetValueOrDefault("cmptype") ?? (tagName.Equals("cmpAction", StringComparison.OrdinalIgnoreCase) ? "Action" : tagName.Equals("cmpDataSet", StringComparison.OrdinalIgnoreCase) ? "DataSet" : "Unknown");
            var sqlText = string.Join("\n", CDataRegex.Matches(body).Select(x => x.Groups["sql"].Value.Trim())).Trim();
            if (string.IsNullOrWhiteSpace(sqlText) && type.Equals("Action", StringComparison.OrdinalIgnoreCase))
            {
                sqlText = ExtractActionInlineSql(body, attrs.GetValueOrDefault("name"), attrs.GetValueOrDefault("mode"));
            }

            if (string.IsNullOrWhiteSpace(sqlText))
                continue;

            if (type.Equals("Script", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var scriptSql in ExtractFromScript(sqlText))
                {
                    result.Add(BuildBlock(filePath, ++i, type, attrs.GetValueOrDefault("name"), attrs.GetValueOrDefault("condition"), scriptSql, $"/fallback/component[{i}]"));
                }
                continue;
            }

            result.Add(BuildBlock(filePath, ++i, type, attrs.GetValueOrDefault("name"), attrs.GetValueOrDefault("condition"), sqlText, $"/fallback/component[{i}]"));
        }

        return result;
    }

    private static IReadOnlyList<string> ExtractFromScript(string script)
    {
        var found = new List<string>();

        foreach (Match match in ScriptSqlRegex.Matches(script))
        {
            var sql = match.Groups["sql"].Value.Trim();
            if (!string.IsNullOrWhiteSpace(sql)) found.Add(sql);
        }

        foreach (Match match in ScriptCallRegex.Matches(script))
        {
            var sql = match.Groups["sql"].Value.Trim();
            if (!string.IsNullOrWhiteSpace(sql)) found.Add(sql + ";");
        }

        return found.Distinct(StringComparer.Ordinal).ToList();
    }

    private static string ExtractActionInlineSql(XElement action)
    {
        return ExtractActionInlineSql(
            string.Concat(action.Nodes().OfType<XText>().Select(t => t.Value)),
            (string?)action.Attribute("name"),
            (string?)action.Attribute("mode"));
    }

    private static string ExtractActionInlineSql(string body, string? name, string? mode)
    {
        if (!string.Equals(name, "SelectAction", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(mode, "post", StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        var beginIndex = body.IndexOf("begin", StringComparison.OrdinalIgnoreCase);
        if (beginIndex < 0)
            return string.Empty;

        var endIndex = body.LastIndexOf("end;", StringComparison.OrdinalIgnoreCase);
        if (endIndex < beginIndex)
            return string.Empty;

        return body.Substring(beginIndex, endIndex + "end;".Length - beginIndex).Trim();
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
