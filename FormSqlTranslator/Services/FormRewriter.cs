using System.Xml.Linq;
using FormSqlTranslator.Models;

namespace FormSqlTranslator.Services;

public sealed class FormRewriter
{
    public string Rewrite(string originalContent, IReadOnlyDictionary<string, string> translatedByBlockId, IReadOnlyList<ExtractedSqlBlock> blocks, string mode)
    {
        var doc = XDocument.Parse(originalContent, LoadOptions.PreserveWhitespace);

        foreach (var block in blocks.Where(b => !b.IsTranslatedBranch))
        {
            if (!translatedByBlockId.TryGetValue(block.BlockId, out var translatedSql))
                continue;

            var parent = FindMatchingComponent(doc, block);
            if (parent is null)
                continue;

            if (block.ComponentType is "Action" or "ActionRouter")
            {
                UpsertBranch(parent, "ActionRouter", translatedSql, mode);
            }
            else if (block.ComponentType is "DataSet" or "SubSelect")
            {
                UpsertBranch(parent, "SubSelect", translatedSql, mode);
            }
        }

        return doc.ToString();
    }

    private static XElement? FindMatchingComponent(XDocument doc, ExtractedSqlBlock block)
    {
        return doc.Descendants("component")
            .FirstOrDefault(e =>
                string.Equals((string?)e.Attribute("cmptype"), block.ComponentType, StringComparison.OrdinalIgnoreCase)
                && string.Equals((string?)e.Attribute("name"), block.ComponentName, StringComparison.Ordinal));
    }

    private static void UpsertBranch(XElement parent, string branchType, string sql, string mode)
    {
        var conditions = ConditionTemplateService.BuildPostgresConditions(mode);

        foreach (var condition in conditions)
        {
            var existing = parent.Elements("component").FirstOrDefault(c =>
                string.Equals((string?)c.Attribute("cmptype"), branchType, StringComparison.OrdinalIgnoreCase) &&
                string.Equals((string?)c.Attribute("condition"), condition, StringComparison.Ordinal));

            if (existing is null)
            {
                existing = new XElement("component",
                    new XAttribute("cmptype", branchType),
                    new XAttribute("condition", condition));
                parent.Add(existing);
            }

            existing.ReplaceNodes(new XCData("\n" + sql.Trim() + "\n"));
        }
    }
}
