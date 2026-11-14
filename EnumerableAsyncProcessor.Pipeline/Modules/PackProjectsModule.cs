using Microsoft.Extensions.Logging;
using ModularPipelines;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Options;
using File = ModularPipelines.FileSystem.FilePath;

namespace EnumerableAsyncProcessor.Pipeline.Modules;

[DependsOn<PackageFilesRemovalModule>]
[DependsOn<NugetVersionGeneratorModule>]
[DependsOn<RunUnitTestsModule>]
[DependsOn<BuildBenchmarkProjectsModule>]
[DependsOn<BuildExampleProjectsModule>]
public class PackProjectsModule : Module<List<CommandResult>>
{
    protected override async Task<List<CommandResult>> ExecuteAsync(IModuleContext context, CancellationToken cancellationToken)
    {
        var results = new List<CommandResult>();
        var packageVersion = await context.GetModule<NugetVersionGeneratorModule>();
        var projectFiles = (await context.Tools.Git.Information.GetRequiredInfoAsync(cancellationToken)).Root.GetFiles(f => GetProjectsPredicate(f, context));

        foreach (var projectFile in projectFiles)
        {
            results.Add(await context.Tools.DotNet.PackAsync(new DotNetPackOptions
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
