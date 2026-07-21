using ModularPipelines.Context;
using ModularPipelines.Git.Extensions;
using ModularPipelines.Modules;

namespace EnumerableAsyncProcessor.Pipeline.Modules;

public class PackageFilesRemovalModule : Module
{
    protected override Task ExecuteModuleAsync(IModuleContext context, CancellationToken cancellationToken)
    {
        var packageFiles = context.Git().RootDirectory.GetFiles(file => file.Extension is ".nupkg");

        foreach (var packageFile in packageFiles)
        {
            packageFile.Delete();
        }

        return Task.CompletedTask;
    }
}
