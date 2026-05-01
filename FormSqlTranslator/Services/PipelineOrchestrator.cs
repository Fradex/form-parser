using FormSqlTranslator.Models;
using Microsoft.Extensions.Logging;

namespace FormSqlTranslator.Services;

public sealed class PipelineOrchestrator(
    FileDiscoveryService discovery,
    FormParser parser,
    SqlTranslatorClient translator,
    ILogger<PipelineOrchestrator> logger)
{
    public async Task RunAsync(CliOptions options, CancellationToken ct)
    {
        Directory.CreateDirectory(options.Output);
        var files = discovery.Discover(options.Input, options.Recursive);
        logger.LogInformation("[FILE] Discovered {Count} files", files.Count);

        await Parallel.ForEachAsync(files, new ParallelOptions { MaxDegreeOfParallelism = options.MaxDegree, CancellationToken = ct }, async (file, token) =>
        {
            try
            {
                var blocks = parser.ExtractBlocks(file);
                logger.LogInformation("[EXTRACT] {File} -> {Count} blocks", file, blocks.Count);

                foreach (var block in blocks)
                {
                    if (options.DryRun)
                        continue;

                    var translated = await translator.TranslateAsync(options.TranslatorUrl, block.Sql, token);
                    var outName = Path.Combine(options.Output, Path.GetFileName(file) + "." + block.Order + ".postgres.sql");
                    await File.WriteAllTextAsync(outName, translated, token);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[ERROR] Failed to process {File}", file);
            }
        });
    }
}
