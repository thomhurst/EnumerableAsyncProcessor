# EnumerableAsyncProcessor

Process Multiple Asynchronous Tasks in Various Ways - One at a time / Batched / Rate limited / Concurrently

[![nuget](https://img.shields.io/nuget/v/EnumerableAsyncProcessor.svg)](https://www.nuget.org/packages/EnumerableAsyncProcessor/)
[![Codacy Badge](https://app.codacy.com/project/badge/Grade/9c57d16dc4a841629560707c5ab3019d)](https://www.codacy.com/gh/thomhurst/EnumerableAsyncProcessor/dashboard?utm_source=github.com&amp;utm_medium=referral&amp;utm_content=thomhurst/EnumerableAsyncProcessor&amp;utm_campaign=Badge_Grade)
[![CodeFactor](https://www.codefactor.io/repository/github/thomhurst/enumerableAsyncProcessor/badge)](https://www.codefactor.io/repository/github/thomhurst/enumerableAsyncProcessor)
<!-- ![Nuget](https://img.shields.io/nuget/dt/EnumerableAsyncProcessor) -->

## Installation

Install via Nuget
`Install-Package EnumerableAsyncProcessor`

## Why I built this

Because I've come across situations where you need to fine tune the rate at which you do things.
Maybe you want it fast.
Maybe you want it slow.
Maybe you want it at a safe balance.
Maybe you just don't want to write all the boilerplate code that comes with managing asynchronous operations!

### Rate Limited Parallel Processor

**Types**  

| Type                                                        | Source Object | Return Object | Method 1            | Method 2           |
|--------------------------------------------------|---------------|---------------|--------------------| ------------------ |
| `RateLimitedParallelAsyncProcessor`                         | ❌             | ❌             | `.WithExecutionCount(int)` | `.ForEachAsync(delegate)` |
| `RateLimitedParallelAsyncProcessor<TInput>`                | ✔             | ❌             | `.WithItems(IEnumerable<TInput>)` | `.ForEachAsync(delegate)` |
| `ResultRateLimitedParallelAsyncProcessor<TOutput>`          | ❌             | ✔             | `.WithExecutionCount(int)` | `.SelectAsync(delegate)`  |
| `ResultRateLimitedParallelAsyncProcessor<TInput, TOutput>` | ✔             | ✔             | `.WithItems(IEnumerable<TInput>)` | `.SelectAsync(delegate)`  |

**How it works**  
Processes your Asynchronous Tasks in Parallel, but honouring the limit that you set. As one finishes, another will start.

E.g. If you set a limit of 100, only 100 should ever run at any one time

This is a hybrid between Parallel Processor and Batch Processor (see below) - Trying to address the caveats of both. Increasing the speed of batching, but not overwhelming the system by using full parallelisation.

**Usage**  

```csharp
var ids = Enumerable.Range(0, 5000).ToList();

// SelectAsync for if you want to return something - using proper disposal
await using var processor = ids
        .SelectAsync(id => DoSomethingAndReturnSomethingAsync(id), CancellationToken.None)
        .ProcessInParallel(levelOfParallelism: 100);
var results = await processor.GetResultsAsync();

// ForEachAsync for when you have nothing to return - using proper disposal  
await using var voidProcessor = ids
        .ForEachAsync(id => DoSomethingAsync(id), CancellationToken.None) 
        .ProcessInParallel(levelOfParallelism: 100);
await voidProcessor.WaitAsync();
```

### Timed Rate Limited Parallel Processor (e.g. Limit RPS)

**Types**  

| Type                                                        | Source Object | Return Object | Method 1            | Method 2           |
|--------------------------------------------------|---------------|---------------|--------------------| ------------------ |
| `TimedRateLimitedParallelAsyncProcessor`                         | ❌             | ❌             | `.WithExecutionCount(int)` | `.ForEachAsync(delegate)` |
| `TimedRateLimitedParallelAsyncProcessor<TInput>`                | ✔             | ❌             | `.WithItems(IEnumerable<TInput>)` | `.ForEachAsync(delegate)` |
| `ResultTimedRateLimitedParallelAsyncProcessor<TOutput>`          | ❌             | ✔             | `.WithExecutionCount(int)` | `.SelectAsync(delegate)`  |
| `ResultTimedRateLimitedParallelAsyncProcessor<TInput, TOutput>` | ✔             | ✔             | `.WithItems(IEnumerable<TInput>)` | `.SelectAsync(delegate)`  |

**How it works**  
Processes your Asynchronous Tasks in Parallel, but honouring the limit that you set over the timespan that you set. As one finishes, another will start, unless you've hit the maximum allowed for the current timespan duration.

E.g. If you set a limit of 100, and a timespan of 1 second, only 100 operation should ever run at any one time over the course of a second. If the operation finishes sooner than a second (or your provided timespan), it'll wait and then start the next operation once that timespan has elapsed.

This is useful in scenarios where, for example, you have an API but it has a request per second limit

**Usage**  

```csharp
var ids = Enumerable.Range(0, 5000).ToList();

// SelectAsync for if you want to return something - using proper disposal
await using var processor = ids
        .SelectAsync(id => DoSomethingAndReturnSomethingAsync(id), CancellationToken.None)
        .ProcessInParallel(levelOfParallelism: 100, TimeSpan.FromSeconds(1));
var results = await processor.GetResultsAsync();

// ForEachAsync for when you have nothing to return - using proper disposal
await using var voidProcessor = ids
        .ForEachAsync(id => DoSomethingAsync(id), CancellationToken.None) 
        .ProcessInParallel(levelOfParallelism: 100, TimeSpan.FromSeconds(1));
await voidProcessor.WaitAsync();
```

**Caveats**  

- If your operations take longer than your provided TimeSpan, you probably won't get your desired throughput. This processor ensures you don't go over your rate limit, but will not increase parallel execution if you're below it.

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

### Parallel

**Types**  

| Type                                             | Source Object | Return Object | Method 1           | Method 2           |
|--------------------------------------------------|---------------|---------------|--------------------| ------------------ |
| `ParallelAsyncProcessor`                         | ❌             | ❌             | `.WithExecutionCount(int)` | `.ForEachAsync(delegate)` |
| `ParallelAsyncProcessor<TInput>`                | ✔             | ❌             | `.WithItems(IEnumerable<TInput>)` | `.ForEachAsync(delegate)` |
| `ResultParallelAsyncProcessor<TOutput>`          | ❌             | ✔             | `.WithExecutionCount(int)` | `.SelectAsync(delegate)`  |
| `ResultParallelAsyncProcessor<TInput, TOutput>` | ✔             | ✔             | `.WithItems(IEnumerable<TInput>)` | `.SelectAsync(delegate)`  |

**How it works**  
Processes your Asynchronous Tasks as fast as it can. All at the same time if it can

**Usage**  

```csharp
var ids = Enumerable.Range(0, 5000).ToList();

// SelectAsync for if you want to return something
var results = await ids
        .SelectAsync(id => DoSomethingAndReturnSomethingAsync(id), CancellationToken.None)
        .ProcessInParallel();

// ForEachAsync for when you have nothing to return
await ids
        .ForEachAsync(id => DoSomethingAsync(id), CancellationToken.None) 
        .ProcessInParallel();
```

**Caveats**  

- Depending on how many operations you have, you could overwhelm your system. Memory and CPU and Network usage could spike, and cause bottlenecks / crashes / exceptions

### Processor Methods

As above, you can see that you can just `await` on the processor to get the results.
Below shows examples of using the processor object and the various methods available.

This is for when you need to Enumerate through some objects and use them in your operations. E.g. Sending notifications to certain ids

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

This is for when you need to don't need any objects - But just want to do something a certain amount of times. E.g. Pinging a site to warm up multiple instances

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

1. **Cancellation**: Internal `CancellationTokenSource` is cancelled
2. **Task Waiting**: Waits up to 30 seconds for running tasks to complete
3. **Resource Cleanup**: Disposes internal resources like `CancellationTokenSource`
4. **Thread Safety**: All disposal operations are thread-safe

### Best Practices

- **Always dispose processors** when you're done with them
- **Use `await using`** when possible for cleaner async disposal
- **Don't worry about double disposal** - it's safe and handled internally
- **Disposal is thread-safe** - multiple threads can safely dispose the same processor
- **Early disposal is safe** - you can dispose while tasks are still running, they'll be cancelled gracefully

### Extension Method Disposal

Note that the convenience extension methods like `IAsyncEnumerable<T>.ProcessInParallel()` handle disposal automatically and return the final results directly:

```csharp
// These extension methods handle disposal internally
var results = await asyncEnumerable.ProcessInParallel();
var transformedResults = await asyncEnumerable.ProcessInParallel(async x => await TransformAsync(x));
```

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
