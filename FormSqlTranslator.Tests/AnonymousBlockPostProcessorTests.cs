using FormSqlTranslator.Services;

namespace FormSqlTranslator.Tests;

public class AnonymousBlockPostProcessorTests
{
    private readonly AnonymousBlockPostProcessor _postProcessor = new();

    [Fact]
    public void Process_ProcedureWrapper_RemovesCallAndCreatesDoBlock()
    {
        const string input = "CREATE OR REPLACE PROCEDURE pg_temp.func_x(INOUT A varchar) AS $anonnym$\nBEGIN\nA := '1';\nEND;\n$anonnym$ LANGUAGE plpgsql;\n\ncall pg_temp.func_x(:A);";

        var processed = _postProcessor.Process(input, isAnonymous: true);

        Assert.Contains("DO $$", processed);
        Assert.DoesNotContain("call pg_temp.func_x", processed, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("LANGUAGE plpgsql", processed, StringComparison.OrdinalIgnoreCase);
    }
}
