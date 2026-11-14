using ModularPipelines;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Options;

namespace EnumerableAsyncProcessor.Pipeline.Modules;

public class BuildExampleProjectsModule : Module<List<CommandResult>>
{
    protected override async Task<List<CommandResult>> ExecuteAsync(
        IModuleContext context,
        CancellationToken cancellationToken)
    {
        var results = new List<CommandResult>();
        var executionOptions = new CommandExecutionOptions
        {
            ThrowOnNonZeroExitCode = true,
        };

        foreach (var exampleProjectFile in (await context.Tools.Git.Information.GetRequiredInfoAsync(cancellationToken)).Root
                     .GetFiles(file => file.Path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
                                       && file.Path.Contains("Example", StringComparison.OrdinalIgnoreCase)))
        {
            results.Add(await context.Tools.DotNet.BuildAsync(new DotNetBuildOptions
            {
                ProjectSolution = exampleProjectFile.Path,
                Configuration = "Release",
            }, executionOptions: executionOptions, cancellationToken: cancellationToken));
        }

        return results;
    }
}
