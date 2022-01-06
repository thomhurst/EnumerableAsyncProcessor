# EnumerableAsyncProcessor
Process Multiple Asynchronous Tasks in Various Ways - One at a time / Batched / Rate limited / Concurrently

[![nuget](https://img.shields.io/nuget/v/TomLonghurst.EnumerableAsyncProcessor.svg)](https://www.nuget.org/packages/TomLonghurst.EnumerableAsyncProcessor/)
[![Codacy Badge](https://app.codacy.com/project/badge/Grade/9c57d16dc4a841629560707c5ab3019d)](https://www.codacy.com/gh/thomhurst/EnumerableAsyncProcessor/dashboard?utm_source=github.com&amp;utm_medium=referral&amp;utm_content=thomhurst/EnumerableAsyncProcessor&amp;utm_campaign=Badge_Grade)
[![CodeFactor](https://www.codefactor.io/repository/github/thomhurst/enumerableAsyncProcessor/badge)](https://www.codefactor.io/repository/github/thomhurst/enumerableAsyncProcessor)
<!-- ![Nuget](https://img.shields.io/nuget/dt/TomLonghurst.EnumerableAsyncProcessor) -->

## Support

If this library helped you, consider buying me a coffee

<a href="https://www.buymeacoffee.com/tomhurst" target="_blank"><img src="https://www.buymeacoffee.com/assets/img/custom_images/orange_img.png" alt="Buy Me A Coffee" style="height: auto !important;width: auto !important;" ></a>

## Installation
.NET 6 Required

Install via Nuget
`Install-Package TomLonghurst.EnumerableAsyncProcessor`

## Why?
Because I've come across situations where you need to fine tune the rate at which you do things.
Maybe you want it fast.
Maybe you want it slow.
Maybe you want it at a safe balance.
Maybe you just don't want to write all the boilerplate code that comes with managing asynchronous operations!

### Rate Limited Parallel Processor

**Types**  
`RateLimitedParallelAsyncProcessor` / `RateLimitedParallelAsyncProcessor<TSource>` / `ResultRateLimitedParallelAsyncProcessor<TResult>` / `ResultRateLimitedParallelAsyncProcessor<TSource, TResult>`

**How it works**  
Processes your Asynchronous Tasks in Parallel, but honouring the limit that you set. As one finishes, another will start. But if you set a limit of 100, only 100 should ever run at any one time

**Usage**  
```csharp
// SelectAsync for if you want to return something
var results = await Enumerable.Range(0, 5000).ToAsyncProcessorBuilder()
        .SelectAsync(id => DoSomethingAndReturnSomethingAsync(id), CancellationToken.None)
        .ProcessInParallel(levelOfParallelism: 100);

// ForEachAsync for when you have nothing to return
await Enumerable.Range(0, 5000).ToAsyncProcessorBuilder()
        .ForEachAsync(id => DoSomethingAsync(id), CancellationToken.None) 
        .ProcessInParallel(levelOfParallelism: 100);
```

### One At A Time

**Types**  
`OneAtATimeAsyncProcessor` / `OneAtATimeAsyncProcessor<TSource>` / `ResultOneAtATimeAsyncProcessor<TResult>` / `ResultOneAtATimeAsyncProcessor<TSource, TResult>`

**How it works**  
Processes your Asynchronous Tasks One at a Time. Only one will ever progress at a time. As one finishes, another will start

**Usage**  
```csharp
// SelectAsync for if you want to return something
var results = await Enumerable.Range(0, 5000).ToAsyncProcessorBuilder()
        .SelectAsync(id => DoSomethingAndReturnSomethingAsync(id), CancellationToken.None)
        .ProcessOneAtATime();

// ForEachAsync for when you have nothing to return
await Enumerable.Range(0, 5000).ToAsyncProcessorBuilder()
        .ForEachAsync(id => DoSomethingAsync(id), CancellationToken.None) 
        .ProcessOneAtATime();
```

**Caveats**  
- Slowest method

### Batch

**Types**  
`BatchAsyncProcessor` / `BatchAsyncProcessor<TSource>` / `ResultBatchAsyncProcessor<TResult>` / `ResultBatchAsyncProcessor<TSource, TResult>`

**How it works**  
Processes your Asynchronous Tasks in Batches. The next batch will not start until every Task in previous batch has finished

**Usage**  
```csharp
// SelectAsync for if you want to return something
var results = await Enumerable.Range(0, 5000).ToAsyncProcessorBuilder()
        .SelectAsync(id => DoSomethingAndReturnSomethingAsync(id), CancellationToken.None)
        .ProcessInBatches(batchSize: 100);

// ForEachAsync for when you have nothing to return
await Enumerable.Range(0, 5000).ToAsyncProcessorBuilder()
        .ForEachAsync(id => DoSomethingAsync(id), CancellationToken.None) 
        .ProcessInBatches(batchSize: 100);
```

**Caveats**  
- If even just 1 Task in a batch is slow or hangs, this will prevent the next batch from starting
- If you set a batch of 100, and 70 have finished, you'll only have 30 left executing. This could slow things down

### Parallel

**Types**  
`ParallelAsyncProcessor` / `ParallelAsyncProcessor<TSource>` / `ResultParallelAsyncProcessor<TResult>` / `ResultParallelAsyncProcessor<TSource, TResult>`

**How it works**  
Processes your Asynchronous Tasks as fast as it can. All at the same time if it can

**Usage**  
```csharp
// SelectAsync for if you want to return something
var results = await Enumerable.Range(0, 5000).ToAsyncProcessorBuilder()
        .SelectAsync(id => DoSomethingAndReturnSomethingAsync(id), CancellationToken.None)
        .ProcessInParallel();

// ForEachAsync for when you have nothing to return
await Enumerable.Range(0, 5000).ToAsyncProcessorBuilder()
        .ForEachAsync(id => DoSomethingAsync(id), CancellationToken.None) 
        .ProcessInParallel();
```

**Caveats**  
- Depending on how many operations you have, you could overwhelm your system. Memory and CPU and Network usage could spike, and cause bottlenecks / crashes / exceptions

### Examples
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

// GetEnumerableTasks() returns IEnumerable<Task<TResult>> - These may have completed, or may still be waiting to finish.
    var tasks = itemProcessor.GetEnumerableTasks();

// Or call GetResultsAsyncEnumerable() to get an IAsyncEnumerable<TResult> so you can process them in real-time as they finish.
    await foreach (var httpResponseMessage in itemProcessor.GetResultsAsyncEnumerable())
    {
        // Do something
    }

// Or call GetResultsAsync() to get a Task<TResult[]> that contains all of the finished results 
    var results = await itemProcessor.GetResultsAsync();

// My dummy method
    Task<HttpResponseMessage> NotifyAsync(int id)
    {
        return httpClient.GetAsync("https://localhost:8080/notify/{id}");
    }
```

This is for when you need to don't need any objects - But just want to do something a certain amount of times. E.g. Pinging a site to warm up multiple instances
```csharp
    var httpClient = new HttpClient();

    var itemProcessor = AsyncProcessorBuilder.WithExecutionCount(100)
        .SelectAsync(PingAsync, CancellationToken.None)
        .ProcessInParallel(10);

// GetEnumerableTasks() returns IEnumerable<Task<TResult>> - These may have completed, or may still be waiting to finish.
    var tasks = itemProcessor.GetEnumerableTasks();

// Or call GetResultsAsyncEnumerable() to get an IAsyncEnumerable<TResult> so you can process them in real-time as they finish.
    await foreach (var httpResponseMessage in itemProcessor.GetResultsAsyncEnumerable())
    {
        // Do something
    }

// Or call GetResultsAsync() to get a Task<TResult[]> that contains all of the finished results 
    var results = await itemProcessor.GetResultsAsync();

// My dummy method
    Task<HttpResponseMessage> PingAsync()
    {
        return httpClient.GetAsync("https://localhost:8080/ping");
    }
```
