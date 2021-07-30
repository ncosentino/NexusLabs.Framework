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
    public static class EventHandlerExtensions
    {
        public static Task InvokeUnorderedAsync<T>(
            this EventHandler<T> @this,
             object sender,
             T eventArgs)
            where T : EventArgs => InvokeUnorderedAsync<T>(
                @this,
                sender,
                eventArgs,
                true);

        public static Task InvokeUnorderedAsync<T>(
            this EventHandler<T> @this,
             object sender,
             T eventArgs,
             bool stopOnFirstError)
            where T : EventArgs => InvokeAsync<T>(
                @this,
                sender,
                eventArgs,
                false,
                stopOnFirstError);

        public static Task InvokeOrderedAsync<T>(
            this EventHandler<T> @this,
             object sender,
             T eventArgs)
            where T : EventArgs => InvokeOrderedAsync<T>(
                 @this,
                 sender,
                 eventArgs,
                 true);

        public static Task InvokeOrderedAsync<T>(
            this EventHandler<T> @this,
             object sender,
             T eventArgs,
             bool stopOnFirstError)
            where T : EventArgs => InvokeAsync<T>(
                @this,
                sender,
                eventArgs,
                true,
                stopOnFirstError);

        public static async Task InvokeAsync<T>(
            this EventHandler<T> @this,
             object sender,
             T eventArgs,
             bool forceOrdering,
             bool stopOnFirstError)
            where T : EventArgs
        {
            if (@this is null)
            {
                return;
            }

            var tcs = new TaskCompletionSource<bool>();

            var delegates = @this.GetInvocationList();
            var count = delegates.Length;
            var caughtException = (Exception)null;
            var setException = (Exception)null;

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
                            if (setException == null)
                            {
                                if (caughtException is null)
                                {
                                    tcs.SetResult(true);
                                }
                                else
                                {
                                    tcs.SetException(caughtException);
                                }

                                setException = caughtException;
                            }
                        }
                    }

                    waitFlag = true;
                });
                var failed = new Action<Exception>(e =>
                {
                    Interlocked.CompareExchange(ref caughtException, e, null);
                });

                if (async)
                {
                    var context = new EventHandlerSynchronizationContext(completed, failed);
                    SynchronizationContext.SetSynchronizationContext(context);
                }

                try
                {
                    @delegate.DynamicInvoke(sender, eventArgs);
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

                if (stopOnFirstError && caughtException != null)
                {
                    lock (tcs)
                    {
                        if (setException == null)
                        {
                            tcs.SetException(caughtException);
                            setException = caughtException;
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
