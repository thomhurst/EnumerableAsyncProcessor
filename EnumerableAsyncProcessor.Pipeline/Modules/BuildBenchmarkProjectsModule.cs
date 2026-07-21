using ModularPipelines.Context;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Git.Extensions;
using ModularPipelines.Models;
using ModularPipelines.Modules;
using ModularPipelines.Options;

namespace EnumerableAsyncProcessor.Pipeline.Modules;

public class BuildBenchmarkProjectsModule : Module<List<CommandResult>>
{
    protected override async Task<List<CommandResult>?> ExecuteAsync(
        IModuleContext context,
        CancellationToken cancellationToken)
    {
        var results = new List<CommandResult>();
        var executionOptions = new CommandExecutionOptions
        {
            ThrowOnNonZeroExitCode = true,
        };

        foreach (var benchmarkProjectFile in context
                     .Git().RootDirectory
                     .GetFiles(file => file.Path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
                                       && file.Path.Contains("Benchmarks", StringComparison.OrdinalIgnoreCase)))
        {
            results.Add(await context.DotNet().Build(new DotNetBuildOptions
            {
                ProjectSolution = benchmarkProjectFile.Path,
                Configuration = "Release",
            }, executionOptions: executionOptions, cancellationToken: cancellationToken).ConfigureAwait(false));
        }

        return results;
    }
}
