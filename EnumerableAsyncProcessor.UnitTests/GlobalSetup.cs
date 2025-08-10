using TUnit.Core.Helpers;

[assembly: Timeout(10_000)]
[assembly: ParallelLimiter<ProcessorCountParallelLimit>]