using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnumerableAsyncProcessor.Builders;
using EnumerableAsyncProcessor.Extensions;

namespace EnumerableAsyncProcessor.UnitTests;

/// <summary>
/// TUnit.Engine (the framework running this suite) is itself compiled against
/// EnumerableAsyncProcessor, and the locally built assembly shadows the version TUnit shipped
/// with because the assembly identity matches. If a member TUnit.Engine binds to disappears,
/// test DISCOVERY crashes with MissingMethodException before any test runs. These are the
/// exact signatures TUnit.Engine references - keep them until TUnit rebuilds against v4.
/// </summary>
public class V3BinaryCompatibilityTests
{
    [Test]
    public async Task Members_Bound_By_TUnit_Engine_Exist_With_Exact_Signatures()
    {
        // ItemActionAsyncProcessorBuilder<TInput, TOutput>.ProcessInParallel(int)
        var builderMethod = typeof(ItemActionAsyncProcessorBuilder<,>).GetMethods()
            .SingleOrDefault(m => m.Name == "ProcessInParallel"
                                  && m.GetParameters().Length == 1
                                  && m.GetParameters()[0].ParameterType == typeof(int));
        await Assert.That(builderMethod).IsNotNull();

        // AsyncEnumerableExtensions.ProcessInParallel<T>(IAsyncEnumerable<T>, CancellationToken)
        var extensionMethod = typeof(AsyncEnumerableExtensions).GetMethods()
            .SingleOrDefault(m => m.Name == "ProcessInParallel"
                                  && m.GetGenericArguments().Length == 1
                                  && m.GetParameters().Length == 2
                                  && m.GetParameters()[1].ParameterType == typeof(CancellationToken));
        await Assert.That(extensionMethod).IsNotNull();

        // TUnit.Engine also binds EnumerableExtensions.SelectAsync / SelectManyAsync and
        // IAsyncProcessor<T>.GetAwaiter; these exact-signature method groups stop compiling if they drift.
        Func<IEnumerable<int>, Func<int, Task<int>>, CancellationToken, ItemActionAsyncProcessorBuilder<int, int>> selectAsync =
            EnumerableExtensions.SelectAsync;
        Func<IEnumerable<int>, Func<int, IAsyncEnumerable<int>>, CancellationToken, IAsyncEnumerable<int>> selectManyAsync =
            EnumerableExtensions.SelectManyAsync;
        await Assert.That(selectAsync).IsNotNull();
        await Assert.That(selectManyAsync).IsNotNull();
    }
}
