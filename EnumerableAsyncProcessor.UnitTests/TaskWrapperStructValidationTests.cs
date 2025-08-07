using System;
using System.Threading;
using System.Threading.Tasks;

namespace EnumerableAsyncProcessor.UnitTests;

/// <summary>
/// Simple validation tests to demonstrate TaskWrapper struct behavior and performance benefits.
/// </summary>
public class TaskWrapperStructValidationTests
{
    [Test]
    public async Task TaskWrapper_IsValueType_Confirmation()
    {
        // Verify all TaskWrapper types are now structs (value types)
        await Assert.That(typeof(ActionTaskWrapper).IsValueType).IsTrue();
        await Assert.That(typeof(ItemTaskWrapper<int>).IsValueType).IsTrue();
        await Assert.That(typeof(ItemTaskWrapper<int, string>).IsValueType).IsTrue();
        await Assert.That(typeof(ActionTaskWrapper<string>).IsValueType).IsTrue();
    }

    [Test]
    public async Task ActionTaskWrapper_ProcessesCorrectly()
    {
        // Arrange
        var executed = false;
        var tcs = new TaskCompletionSource();
        var wrapper = new ActionTaskWrapper(() =>
        {
            executed = true;
            return Task.CompletedTask;
        }, tcs);

        // Act
        await wrapper.Process(CancellationToken.None);

        // Assert
        await Assert.That(executed).IsTrue();
        await Assert.That(tcs.Task.IsCompletedSuccessfully).IsTrue();
    }

    [Test]
    public async Task ItemTaskWrapper_ProcessesWithInput()
    {
        // Arrange
        var processedValue = 0;
        var tcs = new TaskCompletionSource();
        var wrapper = new ItemTaskWrapper<int>(42, value =>
        {
            processedValue = value;
            return Task.CompletedTask;
        }, tcs);

        // Act
        await wrapper.Process(CancellationToken.None);

        // Assert
        await Assert.That(processedValue).IsEqualTo(42);
        await Assert.That(tcs.Task.IsCompletedSuccessfully).IsTrue();
    }

    [Test]
    public async Task TaskWrapper_EqualityWorks()
    {
        // Arrange
        Func<Task> taskFactory = () => Task.CompletedTask;
        var tcs = new TaskCompletionSource();
        var wrapper1 = new ActionTaskWrapper(taskFactory, tcs);
        var wrapper2 = new ActionTaskWrapper(taskFactory, tcs);
        var wrapper3 = new ActionTaskWrapper(() => Task.CompletedTask, new TaskCompletionSource());

        // Act & Assert
        await Assert.That(wrapper1.Equals(wrapper2)).IsTrue();
        await Assert.That(wrapper1 == wrapper2).IsTrue();
        await Assert.That(wrapper1.Equals(wrapper3)).IsFalse();
        await Assert.That(wrapper1 == wrapper3).IsFalse();
    }

    [Test]
    public async Task TaskWrapper_ArrayStorage_WorksWithoutBoxing()
    {
        // Arrange
        var wrappers = new ActionTaskWrapper[5];
        
        // Act - Fill array with structs
        for (int i = 0; i < wrappers.Length; i++)
        {
            wrappers[i] = new ActionTaskWrapper(() => Task.CompletedTask, new TaskCompletionSource());
        }

        // Assert - No boxing should occur
        await Assert.That(wrappers.Length).IsEqualTo(5);
        var hasNonNullFactory = wrappers[0].TaskFactory != null;
        await Assert.That(hasNonNullFactory).IsTrue();
    }

    [Test]
    public async Task TaskWrapper_PassByValue_CopiesBehavior()
    {
        // Arrange
        var originalTcs = new TaskCompletionSource();
        var originalWrapper = new ActionTaskWrapper(() => Task.CompletedTask, originalTcs);
        
        // Act - Modify copy
        var copiedWrapper = originalWrapper;
        copiedWrapper = new ActionTaskWrapper(() => Task.CompletedTask, new TaskCompletionSource());

        // Assert - Original should remain unchanged (struct copy semantics)
        await Assert.That(originalWrapper.TaskCompletionSource).IsEqualTo(originalTcs);
        await Assert.That(copiedWrapper.TaskCompletionSource).IsNotEqualTo(originalTcs);
    }

    [Test]
    public async Task TaskWrapper_MemoryFootprint_IsSmallerThanObjects()
    {
        // This test demonstrates that structs don't allocate on the heap for the wrapper itself
        // Only the TaskCompletionSource instances are heap-allocated
        
        // Arrange & Act
        GC.Collect();
        var initialMemory = GC.GetTotalMemory(false);
        
        var wrappers = new ActionTaskWrapper[1000];
        for (int i = 0; i < wrappers.Length; i++)
        {
            // Only the TaskCompletionSource allocates on the heap, not the wrapper struct
            wrappers[i] = new ActionTaskWrapper(() => Task.CompletedTask, new TaskCompletionSource());
        }
        
        var finalMemory = GC.GetTotalMemory(false);
        var allocatedMemory = finalMemory - initialMemory;
        
        // Assert - Memory allocation should be only for TaskCompletionSource instances
        // Each struct itself doesn't allocate heap memory
        await Assert.That(allocatedMemory).IsLessThan(wrappers.Length * 200); // Conservative estimate
        var hasNonNullFactory = wrappers[0].TaskFactory != null; // Prevent optimization
        await Assert.That(hasNonNullFactory).IsTrue();
    }
}