using FormSqlTranslator.Services;

namespace FormSqlTranslator.Tests;

public class FormParserTests
{
    [Fact]
    public void ExtractBlocks_ParsesScriptComponentSqlCalls()
    {
        var temp = Path.GetTempFileName();
        try
        {
            var content = """
<div>
  <component cmptype="Script"><![CDATA[
    Form.onShow = function(){
      /* D_PKG_OPTION_SPECS.GET('VISIT_PURPOSE_DEFAULT', :LPU_DS) */
      var x = 1;
    }
  ]]></component>
</div>
""";
            File.WriteAllText(temp, content);

            var parser = new FormParser(new SqlBlockClassifier());
            var blocks = parser.ExtractBlocks(temp);

            Assert.NotEmpty(blocks);
            Assert.Contains(blocks, b => b.ComponentType == "Script" && b.Sql.Contains("D_PKG_OPTION_SPECS.GET", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            File.Delete(temp);
        }
    }

    [Fact]
    public void ExtractBlocks_ParsesActionBlockWithAnonymousSql()
    {
        var temp = Path.GetTempFileName();
        try
        {
            var content = """
<div>
  <component cmptype="Action" name="SelectAction"><![CDATA[
    begin
      select dd.D_NAME into :D_NAME from D_V_DIS_DIAPASONES dd where dd.id=:ID;
    end;
  ]]></component>
</div>
""";
            File.WriteAllText(temp, content);

            var parser = new FormParser(new SqlBlockClassifier());
            var blocks = parser.ExtractBlocks(temp);

            var action = Assert.Single(blocks.Where(b => b.ComponentType == "Action"));
            Assert.Contains("begin", action.Sql, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(Models.SqlBlockType.AnonymousBlock, action.BlockType);
        }
        finally
        {
            File.Delete(temp);
        }
    }

    [Fact]
    public void ExtractBlocks_DoesNotTreatInsertStringLiteralAsSqlButKeepsActionCdata()
    {
        var temp = Path.GetTempFileName();
        try
        {
            var content = """
<div>
  <component cmptype="Script"><![CDATA[
    Form.OnSuccessAddUpdate = function ()
    {
      if(getVar("action")=="INSERT")
      {
        setVar("newid",getVar("NEW_ID"),1);
      }
    }
  ]]></component>
  <component cmptype="Action" name="SelectAction"><![CDATA[
    begin
      select GRD_CODE into :GRD_CODE from D_V_DECRETIV_GROUPS t where t.ID = :ID;
    end;
  ]]></component>
</div>
""";
            File.WriteAllText(temp, content);

            var parser = new FormParser(new SqlBlockClassifier());
            var blocks = parser.ExtractBlocks(temp);

            Assert.DoesNotContain(blocks, b => b.ComponentType == "Script");
            var action = Assert.Single(blocks.Where(b => b.ComponentType == "Action"));
            Assert.Contains("from D_V_DECRETIV_GROUPS", action.Sql, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            File.Delete(temp);
        }
    }
}
