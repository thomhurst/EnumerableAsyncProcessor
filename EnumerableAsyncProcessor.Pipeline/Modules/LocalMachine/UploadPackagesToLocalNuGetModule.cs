using EnumerableAsyncProcessor.Extensions;
using Microsoft.Extensions.Logging;
using ModularPipelines.Attributes;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Models;
using ModularPipelines.Modules;

namespace EnumerableAsyncProcessor.Pipeline.Modules.LocalMachine;

[DependsOn<AddLocalNugetSourceModule>]
[DependsOn<PackagePathsParserModule>]
[DependsOn<CreateLocalNugetFolderModule>]
public class UploadPackagesToLocalNuGetModule : Module<CommandResult[]>
{
    protected override async Task OnBeforeExecute(IPipelineContext context)
    {
        var packagePaths = await GetModule<PackagePathsParserModule>();
        foreach (var packagePath in packagePaths.Value!)
        {
            context.Logger.LogInformation("[Local Directory] Uploading {File}", packagePath);
        }

        await base.OnBeforeExecute(context);
    }

    protected override async Task<CommandResult[]?> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken)
    {
        var localRepoLocation = await GetModule<CreateLocalNugetFolderModule>();
        var packagePaths = await GetModule<PackagePathsParserModule>();
        return await packagePaths.Value!.SelectAsync(async file => await context.DotNet()
            .Nuget
            .Push(new DotNetNugetPushOptions(file)
            {
                Source = localRepoLocation.Value!,
            }, cancellationToken), cancellationToken: cancellationToken).ProcessOneAtATime();
    }
}