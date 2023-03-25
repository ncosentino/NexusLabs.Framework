using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace System.Threading.Tasks
{
    public static class TaskExtensions
    {
        public static void Forget(this Task task)
        {
            // do nothing with this guy, but tells callers that we explicitly 
            // don't care about what happens
        }

#if NET6_0_OR_GREATER
        public static async IAsyncEnumerable<TResult> ToUnorderedAsyncEnumerable<TSource, TResult>(
            this IEnumerable<TSource> items,
            Func<TSource, CancellationToken, Task<TResult>> createTask,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
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

        public static async IAsyncEnumerable<TResult> ToOrderedAsyncEnumerable<TSource, TResult>(
            this IEnumerable<TSource> items,
            Func<TSource, CancellationToken, Task<TResult>> createTask,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
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
#endif
    }
}
