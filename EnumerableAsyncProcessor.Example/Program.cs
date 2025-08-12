using EnumerableAsyncProcessor.Builders;
using EnumerableAsyncProcessor.Extensions;
using EnumerableAsyncProcessor.Example;

async Task ItemAsyncProcessor()
{
    var httpClient = new HttpClient();

    var ids = Enumerable.Range(0, 5000).ToList();

    // This is for when you need to Enumerate through some objects and use them in your operations
    
    var itemProcessor = ids.ToAsyncProcessorBuilder()
        .SelectAsync(NotifyAsync, CancellationToken.None)
        .ProcessInParallel(100);

    // Or
    // var itemProcessor = AsyncProcessorBuilder.WithItems(ids)
    //     .SelectAsync(NotifyAsync)
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
}

async Task CountAsyncProcessor()
{
    var httpClient = new HttpClient();

    // This is for when you need to don't need any objects - But just want to do something a certain amount of times. E.g. Pinging a site to warm up multiple instances
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
}

#if NET6_0_OR_GREATER
// Run IAsyncEnumerable examples
Console.WriteLine("\n\n=== Running IAsyncEnumerable Examples ===\n");
await AsyncEnumerableExample.RunExamples();

// Run ProcessInParallel examples
Console.WriteLine("\n\n=== Running ProcessInParallel Extension Examples ===\n");
await ProcessInParallelExample.RunExample();

// Run disposal pattern examples
Console.WriteLine("\n\n=== Running Disposal Pattern Examples ===\n");
await DisposalExample.RunExamples();
#endif