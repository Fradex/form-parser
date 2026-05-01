using FormSqlTranslator.Services;

namespace FormSqlTranslator.Tests;

public class ConditionTemplateServiceTests
{
    [Fact]
    public void BuildPostgresConditions_Both_ReturnsTwoConditions()
    {
        var conditions = ConditionTemplateService.BuildPostgresConditions("both");

        Assert.Equal(2, conditions.Count);
        Assert.Contains("TYPE_DATABASE=POSTGRE&&MODE_DATABASE=tmis", conditions);
        Assert.Contains("TYPE_DATABASE=POSTGRE&&MODE_DATABASE=nmis", conditions);
    }

    [Fact]
    public void BuildPostgresConditions_Tmis_ReturnsOneCondition()
    {
        var conditions = ConditionTemplateService.BuildPostgresConditions("tmis");

        var only = Assert.Single(conditions);
        Assert.Equal("TYPE_DATABASE=POSTGRE&&MODE_DATABASE=tmis", only);
    }
}
