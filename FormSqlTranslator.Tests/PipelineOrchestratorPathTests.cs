using FormSqlTranslator.Services;

namespace FormSqlTranslator.Tests;

public class PipelineOrchestratorPathTests
{
    [Fact]
    public void GetOutputPath_WhenInputIsDirectory_PreservesRelativeStructure()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var input = Path.Combine(root, "in");
        Directory.CreateDirectory(Path.Combine(input, "a", "b"));
        var file = Path.Combine(input, "a", "b", "form.frm");
        File.WriteAllText(file, "x");

        var output = Path.Combine(root, "out");
        var result = PipelineOrchestrator.GetOutputPath(input, output, file);

        Assert.Equal(Path.Combine(output, "a", "b", "form.frm"), result);
    }
}
