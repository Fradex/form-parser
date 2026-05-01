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
}
