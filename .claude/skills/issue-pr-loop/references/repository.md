# EnumerableAsyncProcessor

Read [CLAUDE.md](../../../../CLAUDE.md) for build/test commands and lifecycle contracts.

- Validate all supported target frameworks with the full suite, including the oldest framework's fallback paths.
- Processor, throttling, and scheduling changes need cancellation, exception-propagation, and completion-ordering coverage. Library/pipeline awaits use `ConfigureAwait(false)`; tests need not.
- No Aspire AppHost or external test services. Docker serves only shared lock Redis.
