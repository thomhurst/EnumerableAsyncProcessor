using ModularPipelines;
using ModularPipelines.Context;

namespace EnumerableAsyncProcessor.Pipeline.Modules;

public class PackageFilesRemovalModule : Module
{
    protected override async Task ExecuteAsync(IModuleContext context, CancellationToken cancellationToken)
    {
        var packageFiles = (await context.Tools.Git.Information.GetRequiredInfoAsync(cancellationToken)).Root.GetFiles(file => file.Extension is ".nupkg");

        foreach (var packageFile in packageFiles)
        {
            packageFile.Delete();
        }
    }
}
