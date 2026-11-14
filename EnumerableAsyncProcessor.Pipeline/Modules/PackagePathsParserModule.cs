using ModularPipelines;
using ModularPipelines.Context;
using File = ModularPipelines.FileSystem.FilePath;

namespace EnumerableAsyncProcessor.Pipeline.Modules;

[DependsOn<PackProjectsModule>]
public class PackagePathsParserModule : Module<List<File>>
{
    protected override async Task<List<File>> ExecuteAsync(IModuleContext context, CancellationToken cancellationToken)
    {
        var packPackagesModuleResult = await context.GetModule<PackProjectsModule>();

        return packPackagesModuleResult.ValueOrDefault!
            .Select(x => x.StandardOutput)
            .Select(x => x.Split("Successfully created package '")[1])
            .Select(x => x.Split("'.")[0])
            .Select(x => new File(x))
            .ToList();
    }
}
