using Microsoft.Extensions.Logging;
using ModularPipelines.Attributes;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Git.Extensions;
using ModularPipelines.Models;
using ModularPipelines.Modules;
using File = ModularPipelines.FileSystem.File;

namespace EnumerableAsyncProcessor.Pipeline.Modules;

[DependsOn<PackageFilesRemovalModule>]
[DependsOn<NugetVersionGeneratorModule>]
[DependsOn<RunUnitTestsModule>]
[DependsOn<BuildExampleProjectsModule>]
public class PackProjectsModule : Module<List<CommandResult>>
{
    protected override async Task<List<CommandResult>?> ExecuteAsync(IModuleContext context, CancellationToken cancellationToken)
    {
        var results = new List<CommandResult>();
        var packageVersion = await context.GetModule<NugetVersionGeneratorModule>();
        var projectFiles = context.Git().RootDirectory.GetFiles(f => GetProjectsPredicate(f, context));

        foreach (var projectFile in projectFiles)
        {
            results.Add(await context.DotNet().Pack(new DotNetPackOptions
            {
                ProjectSolution = projectFile.Path,
                Configuration = "Release",
                Properties =
                [
                    ("PackageVersion", packageVersion.ValueOrDefault)!,
                    ("Version", packageVersion.ValueOrDefault)!
                ],
                IncludeSource = true
            }, cancellationToken: cancellationToken));
        }

        return results;
    }

    private bool GetProjectsPredicate(File file, IModuleContext context)
    {
        var path = file.Path;
        if (!path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (path.Contains("Tests", StringComparison.OrdinalIgnoreCase)
            || path.Contains("Benchmarks", StringComparison.OrdinalIgnoreCase)
            || path.Contains("Pipeline", StringComparison.OrdinalIgnoreCase)
            || path.Contains("Example", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        context.Logger.LogInformation("Found File: {File}", path);
        return true;
    }
}
