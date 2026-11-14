using EnumerableAsyncProcessor.Extensions;
using EnumerableAsyncProcessor.Pipeline.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModularPipelines;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Options;

namespace EnumerableAsyncProcessor.Pipeline.Modules;

[DependsOn<RunUnitTestsModule>]
[DependsOn<PackagePathsParserModule>]
public class UploadPackagesToNugetModule : Module<CommandResult[]>
{
    private readonly IOptions<NuGetSettings> _options;

    public UploadPackagesToNugetModule(IOptions<NuGetSettings> options)
    {
        _options = options;
    }

    protected override void Configure(ModuleConfigurationBuilder module)
    {
        module
            .WithSkipWhen(async (context, cancellationToken) =>
            {
                var gitVersionInfo = await context.Tools.Git.Versioning.GetVersioningInformationAsync(cancellationToken);

                if (gitVersionInfo.BranchName != "main")
                {
                    return SkipDecision.Skip("Not on the main branch");
                }

                var publishPackages = Environment.GetEnvironmentVariable("PUBLISH_PACKAGES");

                if (!bool.TryParse(publishPackages, out var shouldPublishPackages) || !shouldPublishPackages)
                {
                    return SkipDecision.Skip("PUBLISH_PACKAGES is not set to true");
                }

                return SkipDecision.When(false, "Publishing packages");
            });
    }

    protected override async Task OnBeforeExecuteAsync(IModuleContext context, CancellationToken cancellationToken)
    {
        var packagePaths = await context.GetModule<PackagePathsParserModule>();

        foreach (var packagePath in packagePaths.ValueOrDefault ?? [])
        {
            context.Logger.LogInformation("Uploading {File}", packagePath);
        }

        await base.OnBeforeExecuteAsync(context, cancellationToken);
    }

    protected override async Task<CommandResult[]> ExecuteAsync(IModuleContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(_options.Value.ApiKey);

        var packagePaths = await context.GetModule<PackagePathsParserModule>();

        return await packagePaths.ValueOrDefault!
            .SelectAsync(file => context.Tools.DotNet
                .NuGet
                .PushAsync(new DotNetNuGetPushOptions([file.Path])
                {
                    Source = "https://api.nuget.org/v3/index.json",
                    ApiKey = _options.Value.ApiKey!
                }, cancellationToken: cancellationToken), cancellationToken: cancellationToken)
            .ProcessOneAtATime();
    }
}
