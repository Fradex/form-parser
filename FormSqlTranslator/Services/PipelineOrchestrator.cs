using FormSqlTranslator.Models;
using Microsoft.Extensions.Logging;

namespace FormSqlTranslator.Services;

public sealed class PipelineOrchestrator(
    FileDiscoveryService discovery,
    FormParser parser,
    SqlTranslatorClient translator,
    AnonymousBlockPostProcessor postProcessor,
    IntermediateArtifactService artifactService,
    FormRewriter rewriter,
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
                var original = await File.ReadAllTextAsync(file, token);
                if (options.SaveIntermediate)
                    await artifactService.SaveOriginalAsync(options.Output, file, original, token);

                var blocks = parser.ExtractBlocks(file);
                logger.LogInformation("[EXTRACT] {File} -> {Count} blocks", file, blocks.Count);

                var translatedByBlock = new Dictionary<string, string>(StringComparer.Ordinal);

                foreach (var block in blocks.Where(b => !b.IsTranslatedBranch))
                {
                    if (options.SaveIntermediate)
                        await artifactService.SaveBlockAsync(options.Output, block, "20-blocks", "oracle.sql", block.Sql, token);

                    if (options.DryRun)
                        continue;

                    var translated = await translator.TranslateAsync(options.TranslatorUrl, block.Sql, token);
                    if (options.SaveIntermediate)
                        await artifactService.SaveBlockAsync(options.Output, block, "30-translation", "postgres.raw.sql", translated, token);

                    var processed = postProcessor.Process(translated, block.BlockType == SqlBlockType.AnonymousBlock);
                    translatedByBlock[block.BlockId] = processed;
                    if (options.SaveIntermediate)
                        await artifactService.SaveBlockAsync(options.Output, block, "40-postprocess", "postgres.processed.sql", processed, token);

                    logger.LogInformation("[TRANSLATE] {File} block {BlockId} done", file, block.BlockId);
                }

                if (!options.DryRun)
                {
                    try
                    {
                        var rewritten = rewriter.Rewrite(original, translatedByBlock, blocks, options.Mode);
                        var rewrittenPath = GetOutputPath(options.Input, options.Output, file);
                        Directory.CreateDirectory(Path.GetDirectoryName(rewrittenPath)!);
                        await File.WriteAllTextAsync(rewrittenPath, rewritten, token);
                        logger.LogInformation("[REWRITE] {File} saved to {OutFile}", file, rewrittenPath);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "[REWRITE] Skipped rewrite for non-xml form {File}", file);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[ERROR] Failed to process {File}", file);
            }
        });
    }

    public static string GetOutputPath(string inputPath, string outputRoot, string filePath)
    {
        if (Directory.Exists(inputPath))
        {
            var root = Path.GetFullPath(inputPath);
            var full = Path.GetFullPath(filePath);
            var relative = Path.GetRelativePath(root, full);
            return Path.Combine(outputRoot, relative);
        }

        return Path.Combine(outputRoot, Path.GetFileName(filePath));
    }
}
