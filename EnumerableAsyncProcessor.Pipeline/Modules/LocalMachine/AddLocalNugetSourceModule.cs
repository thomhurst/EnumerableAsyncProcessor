using ModularPipelines.Attributes;
using ModularPipelines.Configuration;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Exceptions;
using ModularPipelines.Models;
using ModularPipelines.Modules;

namespace EnumerableAsyncProcessor.Pipeline.Modules.LocalMachine;

[DependsOn<CreateLocalNugetFolderModule>]
public class AddLocalNugetSourceModule : Module<CommandResult>
{
    protected override ModuleConfiguration Configure()
    {
        return ModuleConfiguration.Create()
            .WithIgnoreFailuresWhen((_, exception) =>
                exception is CommandException commandException &&
                commandException.StandardOutput.Contains("The name specified has already been added to the list of available package sources"))
            .Build();
    }

    protected override async Task<CommandResult?> ExecuteAsync(IModuleContext context, CancellationToken cancellationToken)
    {
        var localNugetPathResult = await context.GetModule<CreateLocalNugetFolderModule>();

        return await context.DotNet().Nuget.Add
            .Source(new DotNetNugetAddSourceOptions
            {
                Packagesourcepath = localNugetPathResult.ValueOrDefault!.Path,
                Name = "ModularPipelinesLocalNuGet"
            }, cancellationToken: cancellationToken);
    }
}
