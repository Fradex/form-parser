using FormSqlTranslator.Services;

namespace FormSqlTranslator.Tests;

public class AnonymousBlockPostProcessorTests
{
    private readonly AnonymousBlockPostProcessor _postProcessor = new();

    [Fact]
    public void Process_TempWrapper_TransformsToDoBlockAndRemovesWrapperArtifacts()
    {
        const string input = "CREATE OR REPLACE PROCEDURE pg_temp.func_x(INOUT PL2PG_VAR_D_NAME varchar, INOUT B bigint) AS $anonnym$\nDECLARE\n    PL2PG_VAR_LOCAL numeric(17);\nBEGIN\n    PL2PG_VAR_D_NAME := '1';\nEND;\n$anonnym$ LANGUAGE plpgsql;\n\nCALL pg_temp.func_x(:A, :B);";

        var processed = _postProcessor.Process(input, isAnonymous: true);

        Assert.StartsWith("DO $$", processed, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("DECLARE", processed);
        Assert.Contains("D_NAME varchar;", processed);
        Assert.Contains("B bigint;", processed);
        Assert.Contains("LOCAL numeric(17);", processed);
        Assert.DoesNotContain("PL2PG_VAR_", processed, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("CREATE OR REPLACE PROCEDURE", processed, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("CALL pg_temp.func_x", processed, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("LANGUAGE plpgsql", processed, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Process_RealProcedure_DoesNotConvertToDoBlock()
    {
        const string input = "CREATE OR REPLACE PROCEDURE public.save_bulletin(IN p_id bigint) AS $$ BEGIN NULL; END; $$ LANGUAGE plpgsql;";

        var processed = _postProcessor.Process(input, isAnonymous: true);

        Assert.Contains("CREATE OR REPLACE PROCEDURE public.save_bulletin", processed, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DO $$", processed, StringComparison.OrdinalIgnoreCase);
    }
}
