using System;
using System.Threading;
using System.Threading.Tasks;
using ModularPipelines.Attributes;
using ModularPipelines.Context;
using ModularPipelines.Exceptions;
using ModularPipelines.Models;
using ModularPipelines.Modules;
using ModularPipelines.NuGet.Extensions;
using ModularPipelines.NuGet.Options;

namespace EnumerableAsyncProcessor.Pipeline.Modules.LocalMachine;

[DependsOn<CreateLocalNugetFolderModule>]
public class AddLocalNugetSourceModule : Module<CommandResult>
{
    protected override async Task<bool> ShouldIgnoreFailures(IPipelineContext context, Exception exception)
    {
        await Task.Yield();
        return exception is CommandException commandException &&
                               commandException.StandardOutput.Contains("The name specified has already been added to the list of available package sources");
    }

    protected override async Task<CommandResult?> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken)
    {
        var localNugetPathResult = await GetModule<CreateLocalNugetFolderModule>();

        return await context.NuGet()
            .AddSource(new NuGetSourceOptions(new Uri(localNugetPathResult.Value!), "ModularPipelinesLocalNuGet"));
    }
}
