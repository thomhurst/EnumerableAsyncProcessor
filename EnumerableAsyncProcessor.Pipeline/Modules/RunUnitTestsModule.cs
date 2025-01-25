using ModularPipelines.Context;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Git.Extensions;
using ModularPipelines.Enums;
using ModularPipelines.Models;
using ModularPipelines.Modules;

namespace EnumerableAsyncProcessor.Pipeline.Modules;

public class RunUnitTestsModule : Module<List<CommandResult>>
{
    protected override async Task<List<CommandResult>?> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken)
    {
        var results = new List<CommandResult>();

        foreach (var unitTestProjectFile in context
                     .Git().RootDirectory!
                     .GetFiles(file => file.Path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
                                       && file.Path.Contains("UnitTests", StringComparison.OrdinalIgnoreCase)))
        {
            results.Add(await context.DotNet().Test(new DotNetTestOptions
            {
                ProjectSolutionDirectoryDllExe = unitTestProjectFile.Path,
                CommandLogging = CommandLogging.Input | CommandLogging.Error,
            }, cancellationToken));
        }

        return results;
    }
}
