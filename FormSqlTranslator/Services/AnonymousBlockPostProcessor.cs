using System.Text.RegularExpressions;

namespace FormSqlTranslator.Services;

public sealed class AnonymousBlockPostProcessor
{
    private static readonly Regex ProcedureBlockRegex = new(
        @"CREATE\s+OR\s+REPLACE\s+(PROCEDURE|FUNCTION)\s+[^\n]+\s+AS\s+\$anonnym\$(?<body>[\s\S]*?)\$anonnym\$\s+LANGUAGE\s+plpgsql\s*;",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex TrailingCallRegex = new(@"(?im)^\s*call\s+pg_temp\.[\w]+\s*\([\s\S]*?\)\s*;\s*$", RegexOptions.Compiled);
    private static readonly Regex InOutRegex = new(@"\b(IN|OUT|INOUT)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public string Process(string translatedSql, bool isAnonymous)
    {
        var sql = translatedSql.Trim();
        if (!isAnonymous)
            return sql;

        // remove outer noisy wrappers if already wrapped by previous pass
        sql = sql.Replace("DO $$\nBEGIN\n", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
        sql = sql.Replace("\nEND;", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();

        var match = ProcedureBlockRegex.Match(sql);
        if (match.Success)
        {
            var body = match.Groups["body"].Value.Trim();
            body = InOutRegex.Replace(body, string.Empty);
            var result = $"DO $$\nDECLARE\n{body}\n$$;";
            result = TrailingCallRegex.Replace(result, string.Empty).Trim();
            return result;
        }

        sql = TrailingCallRegex.Replace(sql, string.Empty).Trim();
        sql = Regex.Replace(sql, @"(?im)^\s*CREATE\s+OR\s+REPLACE\s+(PROCEDURE|FUNCTION)[\s\S]*?LANGUAGE\s+plpgsql\s*;", string.Empty);

        if (!sql.StartsWith("DO $$", StringComparison.OrdinalIgnoreCase))
            sql = "DO $$\nBEGIN\n" + sql + "\nEND;\n$$;";

        // final cleanup: never keep pg_temp call/function declarations
        sql = Regex.Replace(sql, @"(?im)^\s*call\s+pg_temp\.[\w]+\s*\([\s\S]*?\)\s*;", string.Empty).Trim();
        sql = Regex.Replace(sql, @"(?im)^\s*CREATE\s+OR\s+REPLACE\s+(PROCEDURE|FUNCTION)\b[\s\S]*?LANGUAGE\s+plpgsql\s*;", string.Empty).Trim();

        return sql;
    }
}
