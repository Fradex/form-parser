using System.Text;
using System.Text.RegularExpressions;

namespace FormSqlTranslator.Services;

public sealed class AnonymousBlockPostProcessor
{
    private static readonly Regex WrapperRegex = new(
        @"CREATE\s+OR\s+REPLACE\s+(?<kind>PROCEDURE|FUNCTION)\s+(?<name>pg_temp\.func_[a-zA-Z0-9_]+)\s*\((?<params>[\s\S]*?)\)\s*AS\s*\$(?<tag>anonnym|anonymous)\$\s*(?<body>[\s\S]*?)\$(?<tag2>anonnym|anonymous)\$\s*LANGUAGE\s+plpgsql\s*;",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex InvokeRegex = new(
        @"(?im)^\s*(CALL|SELECT)\s+pg_temp\.func_[a-zA-Z0-9_]+\s*\([\s\S]*?\)\s*;\s*$",
        RegexOptions.Compiled);

    public string Process(string translatedSql, bool isAnonymous)
    {
        var sql = translatedSql.Trim();
        if (!isAnonymous)
            return sql;

        var match = WrapperRegex.Match(sql);
        if (match.Success && InvokeRegex.IsMatch(sql))
        {
            var declareBlock = BuildDeclareBlock(match.Groups["params"].Value);
            var body = match.Groups["body"].Value.Trim();

            var output = new StringBuilder();
            output.AppendLine("DO $$");
            output.AppendLine("DECLARE");
            output.Append(declareBlock);
            output.AppendLine("BEGIN");
            output.AppendLine(body);
            output.AppendLine("END;");
            output.Append("$$;");

            return output.ToString().Trim();
        }

        return CleanupResidualWrapper(sql);
    }

    private static string BuildDeclareBlock(string rawParams)
    {
        var parts = rawParams.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var sb = new StringBuilder();

        foreach (var part in parts)
        {
            // IN/OUT/INOUT var_name type -> var_name type;
            var cleaned = Regex.Replace(part, @"\b(INOUT|IN|OUT)\b", string.Empty, RegexOptions.IgnoreCase).Trim();
            if (string.IsNullOrWhiteSpace(cleaned))
                continue;

            sb.Append("    ").Append(cleaned).AppendLine(";");
        }

        return sb.ToString();
    }

    private static string CleanupResidualWrapper(string sql)
    {
        sql = InvokeRegex.Replace(sql, string.Empty);
        sql = Regex.Replace(sql, @"(?im)^\s*CREATE\s+OR\s+REPLACE\s+(FUNCTION|PROCEDURE)\s+pg_temp\.func_[a-zA-Z0-9_]+[\s\S]*?LANGUAGE\s+plpgsql\s*;", string.Empty);
        return sql.Trim();
    }
}
