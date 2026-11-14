using ModularPipelines;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Options;

namespace EnumerableAsyncProcessor.Pipeline.Modules;

public class RunUnitTestsModule : Module<List<CommandResult>>
{
    protected override async Task<List<CommandResult>> ExecuteAsync(IModuleContext context, CancellationToken cancellationToken)
    {
        var results = new List<CommandResult>();

        foreach (var unitTestProjectFile in (await context.Tools.Git.Information.GetRequiredInfoAsync(cancellationToken)).Root
                     .GetFiles(file => file.Path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
                                       && file.Path.Contains("UnitTests", StringComparison.OrdinalIgnoreCase)))
        {
            results.Add(await context.Tools.DotNet.TestAsync(new DotNetTestOptions
            {
                Project = unitTestProjectFile.Path,
            }, cancellationToken: cancellationToken));
        }

        return results;
    }
}
