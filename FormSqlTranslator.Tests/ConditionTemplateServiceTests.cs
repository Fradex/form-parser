using FormSqlTranslator.Services;

namespace FormSqlTranslator.Tests;

public class ConditionTemplateServiceTests
{
    [Fact]
    public void BuildPostgresConditions_Both_ReturnsOnlyTmisCondition()
    {
        var conditions = ConditionTemplateService.BuildPostgresConditions("both");

        var only = Assert.Single(conditions);
        Assert.Equal("TYPE_DATABASE=POSTGRE&&MODE_DATABASE=tmis", only);
    }

    [Fact]
    public void BuildPostgresConditions_Tmis_ReturnsOneCondition()
    {
        var conditions = ConditionTemplateService.BuildPostgresConditions("tmis");

        var only = Assert.Single(conditions);
        Assert.Equal("TYPE_DATABASE=POSTGRE&&MODE_DATABASE=tmis", only);
    }
}
