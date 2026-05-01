using System.Xml.Linq;
using FormSqlTranslator.Models;

namespace FormSqlTranslator.Services;

public sealed class FormParser(SqlBlockClassifier classifier)
{
    public IReadOnlyList<ExtractedSqlBlock> ExtractBlocks(string filePath)
    {
        var text = File.ReadAllText(filePath);
        XDocument doc;
        try
        {
            doc = XDocument.Parse(text, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
        }
        catch
        {
            return []; // TODO: fallback tolerant parser
        }

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
            result.Add(new ExtractedSqlBlock(
                BlockId: $"{Path.GetFileName(filePath)}-{++i}",
                FilePath: filePath,
                ComponentType: type,
                ComponentName: (string?)n.Attribute("name"),
                Condition: (string?)n.Attribute("condition"),
                Order: i,
                Sql: sql,
                OriginPath: n.GetAbsoluteXPath(),
                BlockType: classifier.Classify(sql)));
        }

        return result;
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
