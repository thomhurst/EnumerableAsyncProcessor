# Contributing

## Public API changes

`Microsoft.CodeAnalysis.PublicApiAnalyzers` tracks the library's public surface. A build fails when public members change without a matching baseline update.

For an intentional API change:

1. Add new declarations to `EnumerableAsyncProcessor/PublicAPI.Unshipped.txt`.
2. Prefix removed declarations with `*REMOVED*` in `PublicAPI.Unshipped.txt`.
3. Include the baseline changes in the same pull request as the code change so reviewers can inspect the public API diff.
4. Move approved declarations from `PublicAPI.Unshipped.txt` to `PublicAPI.Shipped.txt` when preparing a release. Keep `#nullable enable` as the first line in both files.

Run `dotnet build` to verify the baseline on every target framework.
