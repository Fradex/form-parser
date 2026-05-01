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
        UpsertSqlNode(oracleBranch, originalSql, replaceAllSqlNodes: false);

        foreach (var condition in ConditionTemplateService.BuildPostgresConditions(mode))
        {
            var pgBranch = FindOrCreateBranch(container, originalComponent, attributes, condition);
            UpsertSqlNode(pgBranch, translatedSql, replaceAllSqlNodes: true);
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

        var branch = new XElement(
            "component",
            baseAttributes.Select(a => new XAttribute(a.Name, a.Value)),
            originalComponent.Nodes().Select(CloneNode));
        branch.SetAttributeValue("condition", condition);

        originalComponent.AddBeforeSelf(branch);
        return branch;
    }


    private static XNode CloneNode(XNode node) =>
        node switch
        {
            XElement e => new XElement(e),
            XCData c => new XCData(c.Value),
            XText t => new XText(t.Value),
            XComment c => new XComment(c.Value),
            XProcessingInstruction p => new XProcessingInstruction(p.Target, p.Data),
            XDocumentType d => new XDocumentType(d.Name, d.PublicId, d.SystemId, d.InternalSubset),
            _ => throw new NotSupportedException($"Unsupported XML node type: {node.GetType().FullName}")
        };

    private static void UpsertSqlNode(XElement branch, string sql, bool replaceAllSqlNodes)
    {
        var normalizedSql = "\n" + sql.Trim() + "\n";
        if (replaceAllSqlNodes)
        {
            var sqlLikeNodes = branch.Nodes()
                .Where(n => n is XCData || n is XText)
                .ToList();

            if (sqlLikeNodes.Count > 0)
            {
                sqlLikeNodes[0].ReplaceWith(new XCData(normalizedSql));
                foreach (var node in sqlLikeNodes.Skip(1))
                    node.Remove();
                return;
            }
        }

        var cdataNode = branch.Nodes().OfType<XCData>().FirstOrDefault();
        if (cdataNode is not null)
        {
            cdataNode.Value = normalizedSql;
            return;
        }

        branch.Add(new XCData(normalizedSql));
    }
}
