using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModularPipelines.Extensions;
using EnumerableAsyncProcessor.Pipeline.Modules;
using EnumerableAsyncProcessor.Pipeline.Modules.LocalMachine;
using EnumerableAsyncProcessor.Pipeline.Settings;

var builder = ModularPipelines.Pipeline.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json")
    .AddUserSecrets<Program>()
    .AddEnvironmentVariables();

builder.Services.Configure<NuGetSettings>(builder.Configuration.GetSection("NuGet"));

if (builder.Environment.IsDevelopment())
{
    builder.AddModule<CreateLocalNugetFolderModule>()
        .AddModule<AddLocalNugetSourceModule>()
        .AddModule<UploadPackagesToLocalNuGetModule>();
}
else
{
    builder.AddModule<UploadPackagesToNugetModule>();
}

builder.AddModule<BuildBenchmarkProjectsModule>()
    .AddModule<BuildExampleProjectsModule>()
    .AddModule<RunUnitTestsModule>()
    .AddModule<NugetVersionGeneratorModule>()
    .AddModule<PackProjectsModule>()
    .AddModule<PackageFilesRemovalModule>()
    .AddModule<PackagePathsParserModule>();

await using var pipeline = builder.Build();
await pipeline.RunAsync();
