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

            var originalComponent = FindMatchingComponent(doc, block);
            if (originalComponent is null)
                continue;

            UpsertBranchesFromOriginal(originalComponent, block.Sql, translatedSql, mode);
        }

        return doc.ToString();
    }

    private static XElement? FindMatchingComponent(XDocument doc, ExtractedSqlBlock block)
    {
        return doc.Descendants("component")
            .FirstOrDefault(e =>
                string.Equals((string?)e.Attribute("cmptype"), block.ComponentType, StringComparison.OrdinalIgnoreCase)
                && string.Equals((string?)e.Attribute("name"), block.ComponentName, StringComparison.Ordinal)
                && e.Attribute("condition") is null);
    }

    private static void UpsertBranchesFromOriginal(XElement originalComponent, string originalSql, string translatedSql, string mode)
    {
        var container = originalComponent.Parent;
        if (container is null)
            return;

        var attributes = originalComponent.Attributes()
            .Where(a => !string.Equals(a.Name.LocalName, "condition", StringComparison.OrdinalIgnoreCase))
            .Select(a => new XAttribute(a.Name, a.Value))
            .ToArray();

        var oracleBranch = FindOrCreateBranch(container, originalComponent, attributes, ConditionTemplateService.OracleCondition);
        oracleBranch.ReplaceNodes(new XCData("\n" + originalSql.Trim() + "\n"));

        foreach (var condition in ConditionTemplateService.BuildPostgresConditions(mode))
        {
            var pgBranch = FindOrCreateBranch(container, originalComponent, attributes, condition);
            pgBranch.ReplaceNodes(new XCData("\n" + translatedSql.Trim() + "\n"));
        }

        originalComponent.Remove();
    }

    private static XElement FindOrCreateBranch(XElement container, XElement originalComponent, IReadOnlyList<XAttribute> baseAttributes, string condition)
    {
        var name = baseAttributes.FirstOrDefault(a => a.Name.LocalName == "name")?.Value;
        var existing = container.Elements("component").FirstOrDefault(c =>
            string.Equals((string?)c.Attribute("condition"), condition, StringComparison.Ordinal)
            && string.Equals((string?)c.Attribute("name"), name, StringComparison.Ordinal)
            && string.Equals((string?)c.Attribute("cmptype"), originalComponent.Attribute("cmptype")?.Value, StringComparison.OrdinalIgnoreCase));

        if (existing is not null)
            return existing;

        var branch = new XElement("component", baseAttributes.Select(a => new XAttribute(a.Name, a.Value)));
        branch.SetAttributeValue("condition", condition);

        originalComponent.AddBeforeSelf(branch);
        return branch;
    }
}
