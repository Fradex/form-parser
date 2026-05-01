using FormSqlTranslator.Models;
using FormSqlTranslator.Services;

namespace FormSqlTranslator.Tests;

public class FormRewriterTests
{
    [Fact]
    public void Rewrite_MovesOriginalSqlToOracleBranch_AndAddsOnlyTmisByDefault()
    {
        const string original = """
<div>
  <component cmptype="Action" name="SelectAction"><![CDATA[
    select 1;
  ]]></component>
</div>
""";

        var block = new ExtractedSqlBlock(
            BlockId: "f-1",
            FilePath: "f.xml",
            ComponentType: "Action",
            ComponentName: "SelectAction",
            Condition: null,
            Order: 1,
            Sql: "select 1;",
            OriginPath: "/div/component",
            BlockType: SqlBlockType.Query,
            IsTranslatedBranch: false);

        var rewriter = new FormRewriter();
        var rewritten = rewriter.Rewrite(
            original,
            new Dictionary<string, string> { ["f-1"] = "select 2;" },
            [block],
            mode: "both");

        Assert.Contains("condition=\"TYPE_DATABASE=ORACLE\"", rewritten);
        Assert.Contains("condition=\"TYPE_DATABASE=POSTGRE&amp;&amp;MODE_DATABASE=tmis\"", rewritten);
        Assert.DoesNotContain("MODE_DATABASE=nmis", rewritten, StringComparison.OrdinalIgnoreCase);
        Assert.Matches(
            "condition=\"TYPE_DATABASE=ORACLE\"[\\s\\S]*condition=\"TYPE_DATABASE=POSTGRE&amp;&amp;MODE_DATABASE=tmis\"",
            rewritten);
    }

    [Fact]
    public void Rewrite_CopiesNestedTags_WhenCreatingConditionalBranches()
    {
        const string original = """
<div>
  <component cmptype="Action" name="SelectAction">
    <condition expression="x > 0" />
    <![CDATA[
      select 1;
    ]]>
  </component>
</div>
""";

        var block = new ExtractedSqlBlock(
            BlockId: "f-1",
            FilePath: "f.xml",
            ComponentType: "Action",
            ComponentName: "SelectAction",
            Condition: null,
            Order: 1,
            Sql: "select 1;",
            OriginPath: "/div/component",
            BlockType: SqlBlockType.Query,
            IsTranslatedBranch: false);

        var rewriter = new FormRewriter();
        var rewritten = rewriter.Rewrite(
            original,
            new Dictionary<string, string> { ["f-1"] = "select 2;" },
            [block],
            mode: "both");

        Assert.Matches(
            "condition=\"TYPE_DATABASE=ORACLE\"[\\s\\S]*<condition expression=\"x &gt; 0\"\\s*/>",
            rewritten);
        Assert.Matches(
            "condition=\"TYPE_DATABASE=POSTGRE&amp;&amp;MODE_DATABASE=tmis\"[\\s\\S]*<condition expression=\"x &gt; 0\"\\s*/>",
            rewritten);
    }
}
