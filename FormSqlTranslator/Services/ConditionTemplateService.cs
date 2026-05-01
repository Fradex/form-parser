namespace FormSqlTranslator.Services;

public static class ConditionTemplateService
{
    public static IReadOnlyList<string> BuildPostgresConditions(string mode) => mode.ToLowerInvariant() switch
    {
        "tmis" => ["TYPE_DATABASE=POSTGRE&&MODE_DATABASE=tmis"],
        "nmis" => ["TYPE_DATABASE=POSTGRE&&MODE_DATABASE=nmis"],
        _ => ["TYPE_DATABASE=POSTGRE&&MODE_DATABASE=tmis"]
    };

    public static string OracleCondition => "TYPE_DATABASE=ORACLE";
}
