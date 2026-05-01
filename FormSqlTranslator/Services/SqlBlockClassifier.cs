using FormSqlTranslator.Models;

namespace FormSqlTranslator.Services;

public sealed class SqlBlockClassifier
{
    public SqlBlockType Classify(string sql)
    {
        var s = sql.Trim().ToLowerInvariant();
        if (s.Contains("exception") || s.StartsWith("begin") || s.StartsWith("declare") || (s.Contains(" into :") && s.Contains(" end")))
            return SqlBlockType.AnonymousBlock;
        return SqlBlockType.PlainSql;
    }
}
