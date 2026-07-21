# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Repository Overview

EnumerableAsyncProcessor is a NuGet library for processing asynchronous tasks with controlled concurrency: one at a time, batched, rate limited, timed rate limited (e.g. requests-per-second), or fully parallel. The library multi-targets `net8.0`, `net9.0`, and `net10.0` and is strong-named (`Directory.Build.props` signs with `strongname.snk`; internals are visible to the test project).

`agents.md` is a symlink to this file (`claude.md` resolves to `CLAUDE.md` on Windows' case-insensitive filesystem).

## Commands

```powershell
# Build
dotnet build

# Run all tests (TUnit on Microsoft.Testing.Platform; global.json wires dotnet test to MTP)
dotnet test

# Run tests for a single target framework directly
dotnet run --project EnumerableAsyncProcessor.UnitTests -f net10.0

# Run a single test — TUnit uses --treenode-filter (/Assembly/Namespace/Class/Method), NOT --filter
dotnet run --project EnumerableAsyncProcessor.UnitTests -f net10.0 -- --treenode-filter "/*/*/*/TestMethodName"

# Run all tests in one class
dotnet run --project EnumerableAsyncProcessor.UnitTests -f net10.0 -- --treenode-filter "/*/*/ParallelAsyncProcessorTests/*"
```

TUnit test projects compile to executables; VSTest-style `dotnet test --filter` does not work. See the `tunit-testing` skill for full filter syntax.

**TUnit itself depends on this library.** `TUnit.Engine` is compiled against EnumerableAsyncProcessor v3, and the locally built assembly shadows the copy TUnit shipped with (same assembly identity), so removing or changing a public member that TUnit.Engine binds to crashes test discovery with `MissingMethodException` before any test runs. The exact signatures TUnit needs are pinned by `V3BinaryCompatibilityTests` and marked as binary-compat members in the source — do not remove them until TUnit ships a build compiled against v4.

CI (`.github/workflows/dotnet.yml`) runs the `EnumerableAsyncProcessor.Pipeline` project (a ModularPipelines app, `dotnet run -c Release` from that directory), which builds, tests, packs, and — on `main` — publishes to NuGet. Versioning comes from GitVersion (`GitVersion.yml` pins `next-version: 4.0.0`; keep that file present, its absence makes ModularPipelines generate a Mainline config that crashes on GitHub PR merge commits).

## Architecture

### Fluent API flow: extensions → builders → runnable processors

1. **Entry points** (`Extensions/EnumerableExtensions.cs`, `Extensions/AsyncEnumerableExtensions.cs`, `Builders/AsyncProcessorBuilder.cs`): `items.SelectAsync(...)` / `items.ForEachAsync(...)`, or `AsyncProcessorBuilder.WithItems(...)` / `.WithExecutionCount(n)` for source-less runs.
2. **Builders** (`Builders/`) capture the items, the delegate, and a `CancellationTokenSource` linked to the caller's token.
3. **Terminal methods** (`ProcessInParallel`, `ProcessInBatches`, `ProcessOneAtATime`) construct the matching processor from `RunnableProcessors/` and immediately call `StartProcessing()` — **processing begins at build time**, not on first await.

### Processor matrix

Processor classes vary along three axes, reflected in naming:

- **Input**: with items (`<TInput>`) vs. execution-count only (non-generic).
- **Output**: `Result*`-prefixed classes (in `RunnableProcessors/ResultProcessors/`) return values via `IAsyncProcessor<TOutput>` (`GetResultsAsync()`, `GetResultsAsyncEnumerable()`, `GetEnumerableTasks()`); unprefixed classes are fire-and-await (`WaitAsync()`).
- **Strategy**: `OneAtATime`, `Batch`, `Parallel`, `TimedRateLimitedParallel`.

`RunnableProcessors/AsyncEnumerable/` holds the `IAsyncEnumerable<T>`-source variants (Parallel, OneAtATime, Batch). File-name suffixes `_1`/`_2` distinguish generic arity (e.g. `BatchAsyncProcessor_1.cs` is `BatchAsyncProcessor<TInput>`).

### Core mechanics (read these before changing behavior)

- **`ProcessorLifecycle.cs`**: owns start/cancel/dispose shared by both base-class hierarchies. `AbstractAsyncProcessorBase` (void) and `ResultAbstractAsyncProcessorBase` (results) cannot share an ancestor because they fan out to differently typed `TaskCompletionSource` lists, so both delegate to this class. Cancellation is registered in `Start`, not the constructor, so a pre-cancelled token can never fire on a partially built instance. `DisposeAsync` waits up to 30 seconds for in-flight tasks; sync `Dispose` cancels without blocking.
- **TCS-per-item**: each item gets a `TaskCompletionSource`; `TaskWrapper.Process` never throws — it completes the item's TCS with the failure/cancellation instead, so one failed item cannot kill the run or leave awaiters hanging.
- **`WorkerPool.cs`**: rate-limited processors run a fixed pool of worker loops claiming items via `Interlocked.Increment` (P `Task.Run` tasks total, not N throttled tasks + semaphore). Timed rate limiting is a shared `TokenBucketRateLimiter` (`System.Threading.RateLimiting`): workers acquire a permit before starting each item, so `permitsPerWindow`/`window` bound the start rate independently of `maxConcurrency`.
- **Multi-targeting**: `EnumerableExtensions.ToIAsyncEnumerable` uses `Task.WhenEach` on `NET9_0_OR_GREATER` and a completion-order-bucket fallback otherwise. The test project targets `net8.0` specifically to exercise the fallback path — don't drop that TFM.

### Disposal contract

All processors implement `IDisposable`/`IAsyncDisposable`; the README documents the patterns users rely on (`await using`, safe double/early disposal). `IAsyncEnumerableProcessor` implementations are single-use and additionally dispose their internal linked `CancellationTokenSource` when `ExecuteAsync` completes; `IAsyncProcessor` objects returned from the builder pattern are the caller's responsibility. Preserve these semantics — there are dedicated regression tests (`DisposalRegressionTests`, `ExceptionFidelityTests`, `InputEnumerationRegressionTests`).
