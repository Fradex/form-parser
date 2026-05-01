using System.Text.RegularExpressions;

namespace FormSqlTranslator.Services;

public sealed class AnonymousBlockPostProcessor
{
    private static readonly Regex HeaderRegex = new(@"CREATE\s+OR\s+REPLACE\s+(PROCEDURE|FUNCTION)\s+[\s\S]*?\$anonnym\$\s*", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex LanguageRegex = new(@"\$anonnym\$\s*LANGUAGE\s+plpgsql\s*;", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex CallRegex = new(@"\bcall\b[\s\S]*?;\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public string Process(string translatedSql, bool isAnonymous)
    {
        if (!isAnonymous)
            return translatedSql.Trim();

        var sql = translatedSql;
        sql = HeaderRegex.Replace(sql, "DO $$\nDECLARE\n");
        sql = LanguageRegex.Replace(sql, "END;\n$$;");
        sql = CallRegex.Replace(sql, string.Empty);

        if (!sql.Contains("DO $$", StringComparison.OrdinalIgnoreCase))
        {
            sql = "DO $$\nBEGIN\n" + translatedSql.Trim() + "\nEND;\n$$;";
        }

        return sql.Trim();
    }
}
