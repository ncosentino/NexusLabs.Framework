using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;

namespace NexusLabs.Framework.Threading.Tasks;

public static class TaskExtensions
{
    public static void Forget(this Task task)
    {
        // do nothing with this guy, but tells callers that we explicitly 
        // don't care about what happens
    }

    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP013:Await in using",
        Justification = "Tasks created against the linked CTS token are awaited inside the iterator " +
                        "(in the loop and in the finally's Task.WhenAll) before the using disposes the " +
                        "CTS. The async iterator preserves this ordering across yield-return suspensions.")]
    // BCL [EnumeratorCancellation] convention requires the default — `await foreach (var x in source.Iter())`
    // and `WithCancellation(token)` both rely on the parameter being optional so the attribute can flow
    // the consumer's token through. Intentional NLF0018 exception per docs/analyzers/NLF0018.md.
#pragma warning disable NLF0018
    public static async IAsyncEnumerable<TResult> ToUnorderedAsyncEnumerable<TSource, TResult>(
        this IEnumerable<TSource> items,
        Func<TSource, CancellationToken, Task<TResult>> createTask,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
#pragma warning restore NLF0018
    {
        using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        var tasks = items.Select(x => createTask(x, cancellationTokenSource.Token));
        var remainingTasks = tasks.ToHashSet();
        try
        {
            while (remainingTasks.Count > 0)
            {
                var completedTask = await Task
                    .WhenAny(remainingTasks)
                    .ConfigureAwait(false);
                remainingTasks.Remove(completedTask);
                var nextResult = completedTask.Result;
                yield return nextResult;
            }
        }
        finally
        {
            cancellationTokenSource.Cancel();

            try
            {
                await Task.WhenAll(remainingTasks).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { }
        }
    }

    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP013:Await in using",
        Justification = "Tasks created against the linked CTS token are awaited inside the iterator " +
                        "(in the loop and in the finally's Task.WhenAll) before the using disposes the " +
                        "CTS. The async iterator preserves this ordering across yield-return suspensions.")]
    // BCL [EnumeratorCancellation] convention requires the default — `await foreach (var x in source.Iter())`
    // and `WithCancellation(token)` both rely on the parameter being optional so the attribute can flow
    // the consumer's token through. Intentional NLF0018 exception per docs/analyzers/NLF0018.md.
#pragma warning disable NLF0018
    public static async IAsyncEnumerable<TResult> ToOrderedAsyncEnumerable<TSource, TResult>(
        this IEnumerable<TSource> items,
        Func<TSource, CancellationToken, Task<TResult>> createTask,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
#pragma warning restore NLF0018
    {
        using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        var tasks = items.Select(x => createTask(x, cancellationTokenSource.Token));
        var remainingTasks = new Queue<Task<TResult>>(tasks);
        try
        {
            while (remainingTasks.Count > 0)
            {
                var nextTask = remainingTasks.Dequeue();
                await nextTask.ConfigureAwait(false);
                var nextResult = nextTask.Result;
                yield return nextResult;
            }
        }
        finally
        {
            cancellationTokenSource.Cancel();

            try
            {
                await Task.WhenAll(remainingTasks).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { }
        }
    }
}
