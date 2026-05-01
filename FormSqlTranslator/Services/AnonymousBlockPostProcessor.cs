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
            var (localDeclare, statements) = ExtractBlockParts(match.Groups["body"].Value);
            statements = NormalizePl2PgVariables(statements);

            var output = new StringBuilder();
            output.AppendLine("DO $$");
            output.AppendLine("DECLARE");
            output.Append(declareBlock);
            output.Append(localDeclare);
            output.AppendLine("BEGIN");
            output.AppendLine(statements);
            output.AppendLine("END;");
            output.Append("$$;");

            return output.ToString().Trim();
        }

        return NormalizePl2PgVariables(CleanupResidualWrapper(sql));
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

            cleaned = NormalizePl2PgVariables(cleaned);
            sb.Append("    ").Append(cleaned).AppendLine(";");
        }

        return sb.ToString();
    }

    private static (string declareBlock, string statements) ExtractBlockParts(string rawBody)
    {
        var body = rawBody.Trim();
        var wrappedBlock = Regex.Match(
            body,
            @"^\s*(?:DECLARE\s*(?<declare>[\s\S]*?))?BEGIN\s*(?<statements>[\s\S]*?)\s*END\s*;?\s*$",
            RegexOptions.IgnoreCase);

        if (!wrappedBlock.Success)
            return (string.Empty, body);

        var localDeclare = wrappedBlock.Groups["declare"].Value.Trim();
        localDeclare = NormalizePl2PgVariables(localDeclare);
        var statements = wrappedBlock.Groups["statements"].Value.Trim();

        if (string.IsNullOrWhiteSpace(localDeclare))
            return (string.Empty, statements);

        return ($"{IndentLines(localDeclare)}\n", statements);
    }

    private static string NormalizePl2PgVariables(string input)
    {
        return Regex.Replace(input, @"\bPL2PG_VAR_(?<name>[A-Za-z0-9_]+)\b", "${name}", RegexOptions.IgnoreCase);
    }

    private static string IndentLines(string block)
    {
        var lines = block.Replace("\r\n", "\n").Split('\n');
        var sb = new StringBuilder();
        foreach (var line in lines)
            sb.Append("    ").AppendLine(line.TrimEnd());

        return sb.ToString().TrimEnd();
    }

    private static string CleanupResidualWrapper(string sql)
    {
        sql = InvokeRegex.Replace(sql, string.Empty);
        sql = Regex.Replace(sql, @"(?im)^\s*CREATE\s+OR\s+REPLACE\s+(FUNCTION|PROCEDURE)\s+pg_temp\.func_[a-zA-Z0-9_]+[\s\S]*?LANGUAGE\s+plpgsql\s*;", string.Empty);
        return sql.Trim();
    }
}
