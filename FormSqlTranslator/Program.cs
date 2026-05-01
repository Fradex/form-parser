using FormSqlTranslator.Models;
using FormSqlTranslator.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var options = CliOptions.Parse(args);

var services = new ServiceCollection();
services.AddLogging(b => b.AddSimpleConsole(o => o.TimestampFormat = "HH:mm:ss "));
services.AddHttpClient<SqlTranslatorClient>();
services.AddSingleton<FileDiscoveryService>();
services.AddSingleton<SqlBlockClassifier>();
services.AddSingleton<FormParser>();
services.AddSingleton<PipelineOrchestrator>();

using var provider = services.BuildServiceProvider();
var orchestrator = provider.GetRequiredService<PipelineOrchestrator>();
await orchestrator.RunAsync(options, CancellationToken.None);
