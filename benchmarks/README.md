# Benchmarks

The suite measures processor coordination overhead and allocations with completed-task and `Task.Yield` selectors at 1,000, 10,000, and 100,000 items.
The timed scenario uses a zero-duration window so the results isolate coordination overhead rather than wall-clock waiting.

Run every benchmark from the repository root:

```shell
dotnet run -c Release --project benchmarks
```

`BenchmarkSwitcher` forwards BenchmarkDotNet command-line options. List or filter cases before a focused comparison:

```shell
dotnet run -c Release --project benchmarks -- --list flat
dotnet run -c Release --project benchmarks -- --filter "*ThrottledParallel*"
```

`UnboundedParallel` is the in-process baseline for ratio columns. To compare code revisions, run the same filter in separate clean worktrees and give each run a distinct artifacts directory:

```shell
dotnet run -c Release --project benchmarks -- --filter "*ResultStreaming*" --artifacts artifacts/baseline
dotnet run -c Release --project benchmarks -- --filter "*ResultStreaming*" --artifacts artifacts/candidate
```

Use Release builds on the same idle machine and compare the generated Markdown or CSV reports under each artifacts directory.
