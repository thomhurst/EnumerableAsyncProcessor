# EnumerableAsyncProcessor

Process Multiple Asynchronous Tasks in Various Ways - One at a time / Batched / Rate limited / Concurrently

[![nuget](https://img.shields.io/nuget/v/EnumerableAsyncProcessor.svg)](https://www.nuget.org/packages/EnumerableAsyncProcessor/)
[![Codacy Badge](https://app.codacy.com/project/badge/Grade/9c57d16dc4a841629560707c5ab3019d)](https://www.codacy.com/gh/thomhurst/EnumerableAsyncProcessor/dashboard?utm_source=github.com&amp;utm_medium=referral&amp;utm_content=thomhurst/EnumerableAsyncProcessor&amp;utm_campaign=Badge_Grade)
[![CodeFactor](https://www.codefactor.io/repository/github/thomhurst/enumerableAsyncProcessor/badge)](https://www.codefactor.io/repository/github/thomhurst/enumerableAsyncProcessor)
<!-- ![Nuget](https://img.shields.io/nuget/dt/EnumerableAsyncProcessor) -->

## Installation

Install via Nuget
`Install-Package EnumerableAsyncProcessor`

## Performance benchmarks

See the [benchmark guide](benchmarks/README.md) to run the BenchmarkDotNet suite, filter scenarios, and compare revisions.

### Supported frameworks

Version 4 requires .NET 8 or later. The package targets and tests `net8.0`, `net9.0`, and `net10.0`.

## Migrating from v3 to v4

Version 4 is a major release. Review these source and behaviour changes before upgrading:

- **.NET 8 is the minimum runtime.** The `net6.0` and `netstandard2.0` targets, the Polyfill dependency, and the `TaskCompletionSource` compatibility shim were removed. The package targets and tests `net8.0`, `net9.0`, and `net10.0`.
- **Parallel processing has one API.** Use `ProcessInParallel(maxConcurrency: 100)` for bounded concurrency and `ProcessInParallel()` for unbounded concurrency. The old non-timed `RateLimitedParallelAsyncProcessor*` types and duplicate overload family were removed. Rename named `levelOfParallelism` arguments to `maxConcurrency` on the `ProcessInParallel` builder family (`InParallelAsync` keeps its `levelOfParallelism` parameter name); replace positional `ProcessInParallel(true)` calls with `ProcessInParallel(scheduleOnThreadPool: true)`.
- **The no-selector `IAsyncEnumerable<T>.ProcessInParallel(...)` overloads were removed.** They only buffered the stream into a list — their `maxConcurrency`/`scheduleOnThreadPool` parameters had no effect. Pass a selector (`items.ProcessInParallel(item => Task.FromResult(item), maxConcurrency)`) instead, or enumerate the stream directly.
- **`IAsyncEnumerableProcessor` moved to `EnumerableAsyncProcessor.Interfaces`** (previously `EnumerableAsyncProcessor.Extensions`) and now implements `IAsyncDisposable`/`IDisposable`. Processors returned by the `IAsyncEnumerable<T>` builder path dispose their internal resources automatically when `ExecuteAsync` completes; they are single-use.
- **Processor and builder classes are sealed.** None of them were externally subclassable in practice (their pipelines hinge on internal members); v4 makes that explicit.
- **Timed processing is a real start-rate limit.** It now uses a shared token bucket instead of holding each worker slot for at least one window. `ProcessInParallel(permitsPerWindow, window, maxConcurrency)` controls start rate and in-flight concurrency independently. The existing two-argument overload remains and uses its first value for both limits.
- **`IEnumerable<T>` input is materialized once when the processor is built.** One-shot enumerables are now supported and side effects run once. Iterator exceptions surface from the terminal builder call, such as `ProcessInParallel(...)`, instead of later from an awaiter.
- **Synchronous disposal no longer waits.** `Dispose()` cancels pending work and releases resources without blocking. Use `await DisposeAsync()` or `await using` when shutdown must wait for in-flight work; the async wait is bounded to 30 seconds.
- **Arbitrary upper limits were removed.** Task counts and batch sizes may exceed 10,000, and time windows may exceed 24 hours. Validity checks remain: counts, batch sizes, concurrency, and permit counts must be positive; time windows cannot be negative.
- **Validation is consistent and eager.** Invalid `maxConcurrency`, parallelism, batch, and timed-rate arguments now throw while the processor is built for both action and result variants.
- **`TaskWrapper` types and processor plumbing are internal.** The `ActionTaskWrapper`/`ItemTaskWrapper` structs and the previously `protected` members of the abstract processor base classes (`TaskWrappers`, `EnumerableTaskCompletionSources`, `CancellationToken`, constructors) were implementation details that could not be meaningfully used outside the library, and are no longer public.
- **Synchronous `InParallelAsync` delegates now run concurrently.** The `Func<TSource, TResult>` and `Action<TSource>` overloads use thread-pool workers instead of executing delegates sequentially on the caller. Do not depend on their previous thread affinity or execution order.
- **An already-cancelled token yields cancelled tasks, not `ArgumentException`.** Building a processor with a token that is already cancelled (or that cancels between building and the terminal call) now produces a processor whose per-item tasks are cancelled; `WaitAsync`/`GetResultsAsync` throw `OperationCanceledException`. Previously this threw `ArgumentException` from an internal constructor.
- **`IAsyncEnumerable<T>` processors preserve every failure.** The `Task` returned by `ExecuteAsync()` now carries all failures via `Task.Exception.InnerExceptions` (awaiting still throws the first), matching the `IEnumerable<T>` processors. Previously only the first failure was observable and the rest were lost.
- **`ExecuteAsync()` enforces single use.** Calling it a second time throws `InvalidOperationException`; calling it after disposal throws `ObjectDisposedException`. Previously a second call failed with a confusing `ObjectDisposedException` from internal plumbing.
- **Abandoning a result stream cancels remaining work.** Breaking out of `await foreach` over `ExecuteAsync()` now cancels the processor's in-flight work and bounds the drain to the 30-second disposal window, instead of silently blocking until every started task finished naturally.

Version 4 also adds cancellation-aware selectors. Use `(item, cancellationToken) => ...` when in-flight work must observe external cancellation, `CancelAll()`, or disposal:

```csharp
await using var processor = ids
    .ForEachAsync(
        async (id, cancellationToken) =>
            await DoSomethingAsync(id, cancellationToken),
        cancellationToken)
    .ProcessInParallel(maxConcurrency: 100);

await processor.WaitAsync();
```

## Execution model

Bounded parallel processors use a fixed set of workers, so coordination work scales with `maxConcurrency` instead of item count. Bounded `IAsyncEnumerable<T>` processing uses a bounded channel to apply source backpressure while preserving result order. Unbounded processing starts work as input is consumed and can place substantial pressure on memory, CPU, network connections, or downstream services.

Timed processors acquire a shared token-bucket permit before starting each operation. The permit rate and maximum in-flight concurrency are separate controls; long-running operations therefore do not reduce permit replenishment. A zero-length window disables start-rate throttling but retains the concurrency limit.

### Result ordering

- `GetResultsAsync()` and `GetEnumerableTasks()` preserve source order.
- `GetResultsAsyncEnumerable()` yields results in completion order.
- `IAsyncEnumerable<T>` streaming (`ExecuteAsync()`): one-at-a-time, batch, and bounded parallel (`maxConcurrency` set) processors yield in source order; unbounded parallel yields in completion order.
- The awaitable `IAsyncEnumerable<T>.ProcessInParallel(selector, ...)` extension returns results in source order.

## Why I built this

Because I've come across situations where you need to fine tune the rate at which you do things.
Maybe you want it fast.
Maybe you want it slow.
Maybe you want it at a safe balance.
Maybe you just don't want to write all the boilerplate code that comes with managing asynchronous operations!

### Parallel Processor (Optional Concurrency Limit)

**Types**  

| Type                                             | Source Object | Return Object | Method 1            | Method 2           |
|--------------------------------------------------|---------------|---------------|---------------------|--------------------|
| `ParallelAsyncProcessor`                         | ❌             | ❌             | `.WithExecutionCount(int)` | `.ForEachAsync(delegate)` |
| `ParallelAsyncProcessor<TInput>`                 | ✔             | ❌             | `.WithItems(IEnumerable<TInput>)` | `.ForEachAsync(delegate)` |
| `ResultParallelAsyncProcessor<TOutput>`          | ❌             | ✔             | `.WithExecutionCount(int)` | `.SelectAsync(delegate)`  |
| `ResultParallelAsyncProcessor<TInput, TOutput>`  | ✔             | ✔             | `.WithItems(IEnumerable<TInput>)` | `.SelectAsync(delegate)`  |

**How it works**  
Processes asynchronous tasks in parallel. Pass `maxConcurrency` to use a fixed worker pool and bound the number of operations running at once, or omit it for unbounded concurrency.

**Usage**  

```csharp
var ids = Enumerable.Range(0, 5000).ToList();

// SelectAsync for if you want to return something - using proper disposal
await using var processor = ids
        .SelectAsync(id => DoSomethingAndReturnSomethingAsync(id), CancellationToken.None)
        .ProcessInParallel(maxConcurrency: 100);
var results = await processor.GetResultsAsync();

// ForEachAsync for when you have nothing to return - using proper disposal  
await using var voidProcessor = ids
        .ForEachAsync(id => DoSomethingAsync(id), CancellationToken.None) 
        .ProcessInParallel(maxConcurrency: 100);
await voidProcessor.WaitAsync();

// Omit maxConcurrency for unbounded parallel processing
await using var unboundedProcessor = ids
        .ForEachAsync(id => DoSomethingAsync(id), CancellationToken.None)
        .ProcessInParallel();
await unboundedProcessor.WaitAsync();
```

Choose a concurrency limit that protects downstream resources. Unbounded processing can increase memory, CPU, and network pressure.

### Timed Rate Limited Parallel Processor (e.g. Limit RPS)

**Types**  

| Type                                                        | Source Object | Return Object | Method 1            | Method 2           |
|--------------------------------------------------|---------------|---------------|--------------------| ------------------ |
| `TimedRateLimitedParallelAsyncProcessor`                         | ❌             | ❌             | `.WithExecutionCount(int)` | `.ForEachAsync(delegate)` |
| `TimedRateLimitedParallelAsyncProcessor<TInput>`                | ✔             | ❌             | `.WithItems(IEnumerable<TInput>)` | `.ForEachAsync(delegate)` |
| `ResultTimedRateLimitedParallelAsyncProcessor<TOutput>`          | ❌             | ✔             | `.WithExecutionCount(int)` | `.SelectAsync(delegate)`  |
| `ResultTimedRateLimitedParallelAsyncProcessor<TInput, TOutput>` | ✔             | ✔             | `.WithItems(IEnumerable<TInput>)` | `.SelectAsync(delegate)`  |

**How it works**  
Uses a shared token bucket to limit how many operations may start in each window. This is useful when a downstream API has a requests-per-second limit.

The two-argument overload uses the same value for permits per window and maximum in-flight concurrency. Use the three-argument overload when those limits should differ.

**Usage**  

```csharp
var ids = Enumerable.Range(0, 5000).ToList();

// SelectAsync for if you want to return something - using proper disposal
await using var processor = ids
        .SelectAsync(id => DoSomethingAndReturnSomethingAsync(id), CancellationToken.None)
        .ProcessInParallel(
                permitsPerWindow: 100,
                window: TimeSpan.FromSeconds(1),
                maxConcurrency: 200);
var results = await processor.GetResultsAsync();

// ForEachAsync for when you have nothing to return - using proper disposal
await using var voidProcessor = ids
        .ForEachAsync(id => DoSomethingAsync(id), CancellationToken.None) 
        .ProcessInParallel(maxConcurrency: 100, timeSpan: TimeSpan.FromSeconds(1));
await voidProcessor.WaitAsync();
```

The first example allows up to 100 starts per second and 200 in-flight operations. The second preserves the source-compatible two-argument shape and applies 100 to both limits.

### One At A Time

**Types**  

| Type                                               | Source Object | Return Object | Method 1            | Method 2           |
|--------------------------------------------------|---------------|---------------|--------------------| ------------------ |
| `OneAtATimeAsyncProcessor`                         | ❌             | ❌             | `.WithExecutionCount(int)` | `.ForEachAsync(delegate)` |
| `OneAtATimeAsyncProcessor<TInput>`                | ✔             | ❌             | `.WithItems(IEnumerable<TInput>)` | `.ForEachAsync(delegate)` |
| `ResultOneAtATimeAsyncProcessor<TOutput>`          | ❌             | ✔             | `.WithExecutionCount(int)` | `.SelectAsync(delegate)`  |
| `ResultOneAtATimeAsyncProcessor<TInput, TOutput>` | ✔             | ✔             | `.WithItems(IEnumerable<TInput>)` | `.SelectAsync(delegate)`  |

**How it works**  
Processes your Asynchronous Tasks One at a Time. Only one will ever progress at a time. As one finishes, another will start

**Usage**  

```csharp
var ids = Enumerable.Range(0, 5000).ToList();

// SelectAsync for if you want to return something
var results = await ids
        .SelectAsync(id => DoSomethingAndReturnSomethingAsync(id), CancellationToken.None)
        .ProcessOneAtATime();

// ForEachAsync for when you have nothing to return
await ids
        .ForEachAsync(id => DoSomethingAsync(id), CancellationToken.None) 
        .ProcessOneAtATime();
```

**Caveats**  

- Slowest method

### Batch

**Types**  

| Type                                          | Source Object | Return Object | Method 1           | Method 2           |
|--------------------------------------------------|---------------|---------------|--------------------| ------------------ |
| `BatchAsyncProcessor`                         | ❌             | ❌             | `.WithExecutionCount(int)` | `.ForEachAsync(delegate)` |
| `BatchAsyncProcessor<TInput>`                | ✔             | ❌             | `.WithItems(IEnumerable<TInput>)` | `.ForEachAsync(delegate)` |
| `ResultBatchAsyncProcessor<TOutput>`          | ❌             | ✔             | `.WithExecutionCount(int)` | `.SelectAsync(delegate)`  |
| `ResultBatchAsyncProcessor<TInput, TOutput>` | ✔             | ✔             | `.WithItems(IEnumerable<TInput>)` | `.SelectAsync(delegate)`  |

**How it works**  
Processes your Asynchronous Tasks in Batches. The next batch will not start until every Task in previous batch has finished

**Usage**  

```csharp
var ids = Enumerable.Range(0, 5000).ToList();

// SelectAsync for if you want to return something
var results = await ids
        .SelectAsync(id => DoSomethingAndReturnSomethingAsync(id), CancellationToken.None)
        .ProcessInBatches(batchSize: 100);

// ForEachAsync for when you have nothing to return
await ids
        .ForEachAsync(id => DoSomethingAsync(id), CancellationToken.None) 
        .ProcessInBatches(batchSize: 100);
```

**Caveats**  

- If even just 1 Task in a batch is slow or hangs, this will prevent the next batch from starting
- If you set a batch of 100, and 70 have finished, you'll only have 30 left executing. This could slow things down

### Processor Methods

As above, you can see that you can just `await` on the processor to get the results.
Below shows examples of using the processor object and the various methods available.

Use an item processor when each operation needs an input value, such as sending notifications to a set of IDs.

```csharp
    var httpClient = new HttpClient();

    var ids = Enumerable.Range(0, 5000).ToList();

    // This is for when you need to Enumerate through some objects and use them in your operations
    
    var itemProcessor = Enumerable.Range(0, 5000).ToAsyncProcessorBuilder()
        .SelectAsync(NotifyAsync)
        .ProcessInParallel(100);

    // Or
    // var itemProcessor = AsyncProcessorBuilder.WithItems(ids)
    //     .SelectAsync(NotifyAsync, CancellationToken.None)
    //     .ProcessInParallel(100);

// GetEnumerableTasks() returns IEnumerable<Task<TOutput>> - These may have completed, or may still be waiting to finish.
    var tasks = itemProcessor.GetEnumerableTasks();

// Or call GetResultsAsyncEnumerable() to get an IAsyncEnumerable<TOutput> so you can process them in real-time as they finish.
    await foreach (var httpResponseMessage in itemProcessor.GetResultsAsyncEnumerable())
    {
        // Do something
    }

// Or call GetResultsAsync() to get a Task<TOutput[]> that contains all of the finished results 
    var results = await itemProcessor.GetResultsAsync();

// My dummy method
    Task<HttpResponseMessage> NotifyAsync(int id)
    {
        return httpClient.GetAsync($"https://localhost:8080/notify/{id}");
    }
```

Use an execution-count processor when an operation should run a fixed number of times without input values, such as warming multiple site instances.

```csharp
    var httpClient = new HttpClient();

    var itemProcessor = AsyncProcessorBuilder.WithExecutionCount(100)
        .SelectAsync(PingAsync, CancellationToken.None)
        .ProcessInParallel(10);

// GetEnumerableTasks() returns IEnumerable<Task<TOutput>> - These may have completed, or may still be waiting to finish.
    var tasks = itemProcessor.GetEnumerableTasks();

// Or call GetResultsAsyncEnumerable() to get an IAsyncEnumerable<TOutput> so you can process them in real-time as they finish.
    await foreach (var httpResponseMessage in itemProcessor.GetResultsAsyncEnumerable())
    {
        // Do something
    }

// Or call GetResultsAsync() to get a Task<TOutput[]> that contains all of the finished results 
    var results = await itemProcessor.GetResultsAsync();

// My dummy method
    Task<HttpResponseMessage> PingAsync()
    {
        return httpClient.GetAsync("https://localhost:8080/ping");
    }
```

## Proper Disposal of Processor Objects

**Important:** All processor objects implement `IDisposable` and `IAsyncDisposable` and should be properly disposed to ensure clean resource cleanup and task cancellation.

### Why Disposal Matters

When you create a processor (e.g., using `ProcessInParallel()`, `ProcessOneAtATime()`, etc.), the processor:
- Manages a `CancellationTokenSource` internally
- Tracks running tasks that may continue executing
- May hold resources that need cleanup

Proper disposal ensures:
- Running tasks are cancelled gracefully
- Internal resources are cleaned up
- No resource leaks occur

### Disposal Patterns

#### 1. Using `await using` for Async Disposal (Recommended)

```csharp
private static async IAsyncEnumerable<int> ProcessDataAsync(int[] input, CancellationToken token)
{
    await using var processor = input
        .SelectAsync(async x => await TransformAsync(x), token)
        .ProcessInParallel();
        
    await foreach (var result in processor.GetResultsAsyncEnumerable())
    {
        yield return result;
    }
    // Processor automatically disposed here
}
```

#### 2. Using `using` for Synchronous Disposal

```csharp
private static async Task<int[]> ProcessDataAsync(int[] input, CancellationToken token)
{
    using var processor = input
        .SelectAsync(async x => await TransformAsync(x), token)
        .ProcessInParallel();
        
    return await processor.GetResultsAsync();
    // Processor automatically disposed here
}
```

#### 3. Manual Disposal with Try-Finally

```csharp
private static async Task<int[]> ProcessDataAsync(int[] input, CancellationToken token)
{
    var processor = input
        .SelectAsync(async x => await TransformAsync(x), token)
        .ProcessInParallel();
        
    try
    {
        return await processor.GetResultsAsync();
    }
    finally
    {
        await processor.DisposeAsync();
    }
}
```

#### 4. Fire-and-Forget with Disposal

```csharp
private static void StartProcessing(int[] input, CancellationToken token)
{
    var processor = input
        .SelectAsync(async x => await TransformAsync(x), token)
        .ProcessInParallel();
        
    // Don't wait for completion but ensure disposal
    _ = Task.Run(async () =>
    {
        try
        {
            await processor.GetResultsAsync();
        }
        finally
        {
            await processor.DisposeAsync();
        }
    }, token);
}
```

### What Happens During Disposal

When a processor is disposed:

1. **Cancellation**: The internal `CancellationTokenSource` is cancelled and unstarted tasks complete as cancelled. Token-aware selectors can cancel in-flight work.
2. **Task Waiting**: `await DisposeAsync()` waits up to 30 seconds for in-flight tasks to finish; synchronous `Dispose()` cancels and releases without blocking.
3. **Resource Cleanup**: Disposes internal resources like `CancellationTokenSource`
4. **Thread Safety**: All disposal operations are thread-safe

### Best Practices

- **Always dispose processors** when you're done with them
- **Use `await using`** when possible for cleaner async disposal
- **Don't worry about double disposal** - it's safe and handled internally
- **Disposal is thread-safe** - multiple threads can safely dispose the same processor
- **Early disposal is safe** - you can dispose while tasks are still running, they'll be cancelled gracefully

### Extension Method Disposal

Note that the convenience extension methods like `IAsyncEnumerable<T>.ProcessInParallel(selector)` handle disposal automatically and return the final results directly:

```csharp
// These extension methods handle disposal internally
var transformedResults = await asyncEnumerable.ProcessInParallel(async x => await TransformAsync(x));
```

Processors built from an `IAsyncEnumerable<T>` source (`IAsyncEnumerableProcessor`) also dispose their internal resources automatically when `ExecuteAsync` completes; `await using` is still supported and safe if `ExecuteAsync` is never called.

The disposal guidance above applies when you're working with the processor objects directly (using the builder pattern).

## Quick Reference: Disposal Patterns

### ❌ INCORRECT (Resource Leak)
```csharp
// DON'T DO THIS - Never disposed!
var processor = input.SelectAsync(transform, token).ProcessInParallel();
return processor.GetResultsAsyncEnumerable();
```

### ✅ CORRECT Patterns

#### Option 1: Await Using (Recommended)
```csharp
await using var processor = input.SelectAsync(transform, token).ProcessInParallel();
return await processor.GetResultsAsync();
```

#### Option 2: Manual Disposal
```csharp
var processor = input.SelectAsync(transform, token).ProcessInParallel();
try {
    return await processor.GetResultsAsync();
} finally {
    await processor.DisposeAsync();
}
```

#### Option 3: Extension Methods (Auto-Disposal)
```csharp
// These handle disposal internally
var results = await asyncEnumerable.ProcessInParallel(transform);
```
