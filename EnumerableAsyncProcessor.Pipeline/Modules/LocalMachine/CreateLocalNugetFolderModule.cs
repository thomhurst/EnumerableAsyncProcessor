using Microsoft.Extensions.Logging;
using ModularPipelines;
using ModularPipelines.Context;
using ModularPipelines.FileSystem;

namespace EnumerableAsyncProcessor.Pipeline.Modules.LocalMachine;

[DependsOn<RunUnitTestsModule>]
[DependsOn<PackagePathsParserModule>]
public class CreateLocalNugetFolderModule : Module<FolderPath>
{
    protected override Task<FolderPath> ExecuteAsync(IModuleContext context, CancellationToken cancellationToken)
    {
        var localNugetRepositoryFolder = context.Files
            .GetFolder(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData))
            .GetFolder("ModularPipelines")
            .GetFolder("LocalNuget")
            .Create();

        context.Logger.LogInformation("Local NuGet Repository Path: {Path}", localNugetRepositoryFolder.Path);

        return Task.FromResult(localNugetRepositoryFolder);
    }
}
