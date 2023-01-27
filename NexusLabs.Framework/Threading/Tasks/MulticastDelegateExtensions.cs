using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;

namespace System.Threading.Tasks
{
    /// <remarks>
    /// This code is largely based on this blog post:
    /// https://olegkarasik.wordpress.com/2019/04/16/code-tip-how-to-work-with-asynchronous-event-handlers-in-c/
    /// and this gist:
    /// https://gist.github.com/OlegKarasik/90c2355e3e170a0885bd06874183428a
    /// 
    /// Honestly, full credit to Oleg Karasik for being able to figure all of 
    /// this out. I've just gone ahead and polished a bit of it up to get the
    /// additional functionality I would expect to have.
    /// </remarks>
    internal static class MulticastDelegateExtensions
    {
        internal static Task InvokeAsync<T>(
            this MulticastDelegate @this,
            object sender,
            T eventArgs,
            bool forceOrdering,
            bool stopOnFirstError)
            where T : EventArgs => InvokeAsync(
                @this,
                forceOrdering,
                stopOnFirstError,
                sender,
                eventArgs);

        internal static async Task InvokeAsync(
            this MulticastDelegate @this,
            bool forceOrdering,
            bool stopOnFirstError,
            params object[] args)
        {
            if (@this is null)
            {
                return;
            }

            var tcs = new TaskCompletionSource<bool>();

            var delegates = @this.GetInvocationList();
            var count = delegates.Length;

            // keep track of exceptions along the way and a separate collection
            // for exceptions we have assigned to the TCS
            var assignedExceptions = new List<Exception>();
            var trackedExceptions = new ConcurrentQueue<Exception>();

            foreach (var @delegate in @this.GetInvocationList())
            {
                var async = @delegate.Method
                    .GetCustomAttributes(typeof(AsyncStateMachineAttribute), false)
                    .Any();

                bool waitFlag = false;
                var completed = new Action(() =>
                {
                    if (Interlocked.Decrement(ref count) == 0)
                    {
                        lock (tcs)
                        {
                            assignedExceptions.AddRange(trackedExceptions);

                            if (!trackedExceptions.Any())
                            {
                                tcs.SetResult(true);
                            }
                            else if (trackedExceptions.Count == 1)
                            {
                                tcs.SetException(assignedExceptions[0]);
                            }
                            else
                            {
                                tcs.SetException(new AggregateException(assignedExceptions));
                            }
                        }
                    }

                    waitFlag = true;
                });
                var failed = new Action<Exception>(e =>
                {
                    trackedExceptions.Enqueue(e);
                });

                if (async)
                {
                    var context = new EventHandlerSynchronizationContext(completed, failed);
                    SynchronizationContext.SetSynchronizationContext(context);
                }

                try
                {
                    @delegate.DynamicInvoke(args);
                }
                catch (TargetParameterCountException e)
                {
                    throw;
                }
                catch (TargetInvocationException e) when (e.InnerException != null)
                {
                    // When exception occured inside Delegate.Invoke method all exceptions are wrapped in
                    // TargetInvocationException.
                    failed(e.InnerException);
                }
                catch (Exception e)
                {
                    failed(e);
                }

                if (!async)
                {
                    completed();
                }

                while (forceOrdering && !waitFlag)
                {
                    await Task.Yield();
                }

                if (stopOnFirstError && trackedExceptions.Any())
                {
                    lock (tcs)
                    {
                        if (!assignedExceptions.Any())
                        {
                            assignedExceptions.AddRange(trackedExceptions);
                            if (trackedExceptions.Count == 1)
                            {
                                tcs.SetException(assignedExceptions[0]);
                            }
                            else
                            {
                                tcs.SetException(new AggregateException(assignedExceptions));
                            }
                        }
                    }

                    break;
                }
            }

            await tcs.Task;
        }

        private sealed class EventHandlerSynchronizationContext : SynchronizationContext
        {
            private readonly Action _completed;
            private readonly Action<Exception> _failed;

            public EventHandlerSynchronizationContext(
                Action completed,
                Action<Exception> failed)
            {
                _completed = completed;
                _failed = failed;
            }

            public override SynchronizationContext CreateCopy()
            {
                return new EventHandlerSynchronizationContext(
                    _completed,
                    _failed);
            }

            public override void Post(SendOrPostCallback d, object state)
            {
                if (state is ExceptionDispatchInfo edi)
                {
                    _failed(edi.SourceException);
                }
                else
                {
                    base.Post(d, state);
                }
            }

            public override void Send(SendOrPostCallback d, object state)
            {
                if (state is ExceptionDispatchInfo edi)
                {
                    _failed(edi.SourceException);
                }
                else
                {
                    base.Send(d, state);
                }
            }

            public override void OperationCompleted() => _completed();
        }
    }
}
