using Microsoft.Extensions.Logging;
using ModularPipelines.Context;
using ModularPipelines.Git.Extensions;
using ModularPipelines.Models;
using ModularPipelines.Modules;

namespace EnumerableAsyncProcessor.Pipeline.Modules;

public class NugetVersionGeneratorModule : Module<string>
{
    protected override async Task<string?> ExecuteAsync(IModuleContext context, CancellationToken cancellationToken)
    {
        var gitVersionInformation = await context.Git().Versioning.GetGitVersioningInformation();

        if (gitVersionInformation.BranchName == "main")
        {
            return gitVersionInformation.SemVer!;
        }

        return $"{gitVersionInformation.Major}.{gitVersionInformation.Minor}.{gitVersionInformation.Patch}-{gitVersionInformation.PreReleaseLabel}-{gitVersionInformation.CommitsSinceVersionSource}";
    }

    protected override Task<ModuleResult<string>?> OnAfterExecuteAsync(IModuleContext context, ModuleResult<string> result, CancellationToken cancellationToken)
    {
        context.Logger.LogInformation("NuGet Version to Package: {Version}", result.ValueOrDefault);
        return base.OnAfterExecuteAsync(context, result, cancellationToken);
    }
}
