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
        EnsureOracleBranch(parent);
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
                InsertBranch(parent, existing, condition);
            }

            existing.ReplaceNodes(new XCData("\n" + sql.Trim() + "\n"));
        }
    }

    private static void InsertBranch(XElement parent, XElement branch, string condition)
    {
        var oracleBranch = parent.Elements("component")
            .FirstOrDefault(c => string.Equals((string?)c.Attribute("condition"), ConditionTemplateService.OracleCondition, StringComparison.Ordinal));

        var isTmis = condition.Contains("MODE_DATABASE=tmis", StringComparison.OrdinalIgnoreCase);
        if (isTmis && oracleBranch is not null)
        {
            oracleBranch.AddAfterSelf(branch);
            return;
        }

        parent.Add(branch);
    }

    private static void EnsureOracleBranch(XElement parent)
    {
        var existingOracle = parent.Elements("component")
            .FirstOrDefault(c =>
                string.Equals((string?)c.Attribute("condition"), ConditionTemplateService.OracleCondition, StringComparison.Ordinal));

        if (existingOracle is not null)
            return;

        var originalSql = string.Concat(parent.Nodes().OfType<XCData>().Select(c => c.Value)).Trim();
        if (string.IsNullOrWhiteSpace(originalSql))
            return;

        parent.Nodes().OfType<XCData>().Remove();

        var oracleBranch = new XElement("component",
            new XAttribute("cmptype", parent.Attribute("cmptype")?.Value ?? string.Empty),
            new XAttribute("condition", ConditionTemplateService.OracleCondition),
            new XCData("\n" + originalSql + "\n"));

        parent.AddFirst(oracleBranch);
    }
}
