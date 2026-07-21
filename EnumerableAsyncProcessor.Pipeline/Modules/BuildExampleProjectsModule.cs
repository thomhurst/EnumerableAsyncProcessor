using ModularPipelines.Context;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Git.Extensions;
using ModularPipelines.Models;
using ModularPipelines.Modules;

namespace EnumerableAsyncProcessor.Pipeline.Modules;

public class BuildExampleProjectsModule : Module<List<CommandResult>>
{
    protected override async Task<List<CommandResult>?> ExecuteAsync(
        IModuleContext context,
        CancellationToken cancellationToken)
    {
        var results = new List<CommandResult>();

        foreach (var exampleProjectFile in context
                     .Git().RootDirectory
                     .GetFiles(file => file.Path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
                                       && file.Path.Contains("Example", StringComparison.OrdinalIgnoreCase)))
        {
            results.Add(await context.DotNet().Build(new DotNetBuildOptions
            {
                ProjectSolution = exampleProjectFile.Path,
                Configuration = "Release",
            }, cancellationToken: cancellationToken));
        }

        return results;
    }
}
