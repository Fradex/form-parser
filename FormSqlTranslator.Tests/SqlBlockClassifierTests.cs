using FormSqlTranslator.Models;
using FormSqlTranslator.Services;

namespace FormSqlTranslator.Tests;

public class SqlBlockClassifierTests
{
    private readonly SqlBlockClassifier _classifier = new();

    [Fact]
    public void Classify_BeginEnd_ReturnsAnonymous()
    {
        var sql = "begin select 1 into :X from dual; end;";

        var type = _classifier.Classify(sql);

        Assert.Equal(SqlBlockType.AnonymousBlock, type);
    }

    [Fact]
    public void Classify_Select_ReturnsPlainSql()
    {
        var sql = "select 1 from dual";

        var type = _classifier.Classify(sql);

        Assert.Equal(SqlBlockType.PlainSql, type);
    }
}
