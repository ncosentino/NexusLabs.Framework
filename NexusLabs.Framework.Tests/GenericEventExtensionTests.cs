using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

using Xunit;

namespace NexusLabs.Framework.Tests
{
    [ExcludeFromCodeCoverage]
    public sealed class GenericEventExtensionTests
    {
        [Fact]
        public async Task InvokeAsync_OrderedStopOnFirstErrorTrueBothAsync_ExecutedInOrder()
        {
            var invoker = new GenericEventHandlerInvoker();

            int firstCount = 0;
            int secondCount = 0;
            invoker.Event += async (_, __) =>
            {
                Assert.Equal(0, secondCount);
                await Task.Run(() => firstCount++).ConfigureAwait(false);
            };
            invoker.Event += async (_, __) =>
            {
                Assert.Equal(1, firstCount);
                await Task.Run(() => secondCount++).ConfigureAwait(false);
            };

            await invoker
                .InvokeAsync(true, true)
                .ConfigureAwait(false);
            Assert.Equal(1, firstCount);
            Assert.Equal(1, secondCount);
        }

        [Fact]
        public async Task InvokeAsync_OrderedStopOnFirstErrorTrueFirstAsync_ExecutedInOrder()
        {
            var invoker = new GenericEventHandlerInvoker();

            int firstCount = 0;
            int secondCount = 0;
            invoker.Event += async (_, __) =>
            {
                Assert.Equal(0, secondCount);
                await Task.Run(() => firstCount++).ConfigureAwait(false);
            };
            invoker.Event += (_, __) =>
            {
                Assert.Equal(1, firstCount);
                secondCount++;
            };

            await invoker
                .InvokeAsync(true, true)
                .ConfigureAwait(false);
            Assert.Equal(1, firstCount);
            Assert.Equal(1, secondCount);
        }

        [Fact]
        public async Task InvokeAsync_OrderedStopOnFirstErrorTrueSecondAsync_ExecutedInOrder()
        {
            var invoker = new GenericEventHandlerInvoker();

            int firstCount = 0;
            int secondCount = 0;
            invoker.Event += (_, __) =>
            {
                Assert.Equal(0, secondCount);
                firstCount++;
            };
            invoker.Event += async (_, __) =>
            {
                Assert.Equal(1, firstCount);
                await Task.Run(() => secondCount++).ConfigureAwait(false);
            };

            await invoker
                .InvokeAsync(true, true)
                .ConfigureAwait(false);
            Assert.Equal(1, firstCount);
            Assert.Equal(1, secondCount);
        }

        [Fact]
        public async Task InvokeAsync_UnorderedStopOnFirstErrorTrueBothAsync_ExecutedInOrder()
        {
            var invoker = new GenericEventHandlerInvoker();

            int firstCount = 0;
            int secondCount = 0;
            invoker.Event += async (_, __) =>
            {
                await Task.Run(() => firstCount++).ConfigureAwait(false);
            };
            invoker.Event += async (_, __) =>
            {
                await Task.Run(() => secondCount++).ConfigureAwait(false);
            };

            await invoker
                .InvokeAsync(false, true)
                .ConfigureAwait(false);
            Assert.Equal(1, firstCount);
            Assert.Equal(1, secondCount);
        }

        [Fact]
        public async Task InvokeAsync_UnorderedStopOnFirstErrorTrueFirstAsync_ExecutedInOrder()
        {
            var invoker = new GenericEventHandlerInvoker();

            int firstCount = 0;
            int secondCount = 0;
            invoker.Event += async (_, __) =>
            {
                await Task.Run(() => firstCount++).ConfigureAwait(false);
            };
            invoker.Event += (_, __) =>
            {
                secondCount++;
            };

            await invoker
                .InvokeAsync(false, true)
                .ConfigureAwait(false);
            Assert.Equal(1, firstCount);
            Assert.Equal(1, secondCount);
        }

        [Fact]
        public async Task InvokeAsync_UnorderedStopOnFirstErrorTrueSecondAsync_ExecutedInOrder()
        {
            var invoker = new GenericEventHandlerInvoker();

            int firstCount = 0;
            int secondCount = 0;
            invoker.Event += (_, __) =>
            {
                firstCount++;
            };
            invoker.Event += async (_, __) =>
            {
                await Task.Run(() => secondCount++).ConfigureAwait(false);
            };

            await invoker
                .InvokeAsync(true, true)
                .ConfigureAwait(false);
            Assert.Equal(1, firstCount);
            Assert.Equal(1, secondCount);
        }
         
        [Fact]
        public async Task InvokeAsync_OrderedStopOnFirstErrorFalseBothAsync_FirstExceptionAllowsExecution()
        {
            var invoker = new GenericEventHandlerInvoker();

            int secondCount = 0;
            invoker.Event += async (_, __) =>
            {
                await Task.Run(() => throw new InvalidOperationException("expected")).ConfigureAwait(false);
            };
            invoker.Event += async (_, __) =>
            {
                await Task.Run(() => secondCount++).ConfigureAwait(false);
            };

            await Assert
                .ThrowsAsync<InvalidOperationException>(async () => await invoker
                    .InvokeAsync(true, false)
                    .ConfigureAwait(false))
                .ConfigureAwait(false);
            Assert.Equal(1, secondCount);
        }

        [Fact]
        public async Task InvokeAsync_OrderedStopOnFirstErrorFalseFirstAsync_FirstExceptionAllowsExecution()
        {
            var invoker = new GenericEventHandlerInvoker();

            int secondCount = 0;
            invoker.Event += async (_, __) =>
            {
                await Task.Run(() => throw new InvalidOperationException("expected")).ConfigureAwait(false);
            };
            invoker.Event += (_, __) =>
            {
                secondCount++;
            };

            await Assert
                .ThrowsAsync<InvalidOperationException>(async () => await invoker
                    .InvokeAsync(true, false)
                    .ConfigureAwait(false))
                .ConfigureAwait(false);
            Assert.Equal(1, secondCount);
        }

        [Fact]
        public async Task InvokeAsync_OrderedStopOnFirstErrorFalseSecondAsync_FirstExceptionAllowsExecution()
        {
            var invoker = new GenericEventHandlerInvoker();

            int secondCount = 0;
            invoker.Event += (_, __) =>
            {
                throw new InvalidOperationException("expected");
            };
            invoker.Event += async (_, __) =>
            {
                await Task.Run(() => secondCount++).ConfigureAwait(false);
            };

            await Assert
                .ThrowsAsync<InvalidOperationException>(async () => await invoker
                    .InvokeAsync(true, false)
                    .ConfigureAwait(false))
                .ConfigureAwait(false);
            Assert.Equal(1, secondCount);
        }

        [Fact]
        public async Task InvokeAsync_OrderedStopOnFirstErrorFalseBothAsync_AllExceptionsCaught()
        {
            var invoker = new GenericEventHandlerInvoker();

            var exception1 = new InvalidOperationException("expected 1");
            invoker.Event += async (_, __) =>
            {
                throw exception1;
            };

            var exception2 = new InvalidOperationException("expected 2");
            invoker.Event += async (_, __) =>
            {
                throw exception2;
            };

            var actualException = await Assert
                .ThrowsAsync<AggregateException>(async () => await invoker
                    .InvokeAsync(true, false)
                    .ConfigureAwait(false))
                .ConfigureAwait(false);
            Assert.Equal(2, actualException.InnerExceptions.Count);
            Assert.Equal(exception1, actualException.InnerExceptions.First());
            Assert.Equal(exception2, actualException.InnerExceptions.Last());
        }

        [Fact]
        public async Task InvokeAsync_UnorderedStopOnFirstErrorFalseBothAsync_FirstExceptionAllowsExecution()
        {
            var invoker = new GenericEventHandlerInvoker();

            int secondCount = 0;
            invoker.Event += async (_, __) =>
            {
                await Task.Run(() => throw new InvalidOperationException("expected")).ConfigureAwait(false);
            };
            invoker.Event += async (_, __) =>
            {
                await Task.Run(() => secondCount++).ConfigureAwait(false);
            };

            await Assert
                .ThrowsAsync<InvalidOperationException>(async () => await invoker
                    .InvokeAsync(false, false)
                    .ConfigureAwait(false))
                .ConfigureAwait(false);
            Assert.Equal(1, secondCount);
        }

        [Fact]
        public async Task InvokeAsync_UnorderedStopOnFirstErrorFalseFirstAsync_FirstExceptionAllowsExecution()
        {
            var invoker = new GenericEventHandlerInvoker();

            int secondCount = 0;
            invoker.Event += async (_, __) =>
            {
                await Task.Run(() => throw new InvalidOperationException("expected")).ConfigureAwait(false);
            };
            invoker.Event += (_, __) =>
            {
                secondCount++;
            };

            await Assert
                .ThrowsAsync<InvalidOperationException>(async () => await invoker
                    .InvokeAsync(false, false)
                    .ConfigureAwait(false))
                .ConfigureAwait(false);
            Assert.Equal(1, secondCount);
        }

        [Fact]
        public async Task InvokeAsync_UnorderedStopOnFirstErrorFalseSecondAsync_FirstExceptionAllowsExecution()
        {
            var invoker = new GenericEventHandlerInvoker();

            int secondCount = 0;
            invoker.Event += (_, __) =>
            {
                throw new InvalidOperationException("expected");
            };
            invoker.Event += async (_, __) =>
            {
                await Task.Run(() => secondCount++).ConfigureAwait(false);
            };

            await Assert
                .ThrowsAsync<InvalidOperationException>(async () => await invoker
                    .InvokeAsync(false, false)
                    .ConfigureAwait(false))
                .ConfigureAwait(false);
            Assert.Equal(1, secondCount);
        }

        [Fact]
        public async Task InvokeAsync_OrderedStopOnFirstErrorTrueBothAsync_FirstExceptionPreventsExecution()
        {
            var invoker = new GenericEventHandlerInvoker();

            int secondCount = 0;
            invoker.Event += async (_, __) =>
            {
                await Task.Run(() => throw new InvalidOperationException("expected")).ConfigureAwait(false);
            };
            invoker.Event += async (_, __) =>
            {
                await Task.Run(() => secondCount++).ConfigureAwait(false);
            };

            var actualException = await Assert
                .ThrowsAsync<InvalidOperationException>(async () => await invoker
                    .InvokeAsync(true, true)
                    .ConfigureAwait(false))
                .ConfigureAwait(false);
            Assert.Equal("expected", actualException.Message);
            Assert.Equal(0, secondCount);
        }

        [Fact]
        public async Task InvokeAsync_OrderedStopOnFirstErrorTrueFirstAsync_FirstExceptionPreventsExecution()
        {
            var invoker = new GenericEventHandlerInvoker();

            int secondCount = 0;
            invoker.Event += async (_, __) =>
            {
                await Task.Run(() => throw new InvalidOperationException("expected")).ConfigureAwait(false);
            };
            invoker.Event += (_, __) =>
            {
                secondCount++;
            };

            var actualException = await Assert
                .ThrowsAsync<InvalidOperationException>(async () => await invoker
                    .InvokeAsync(true, true)
                    .ConfigureAwait(false))
                .ConfigureAwait(false);
            Assert.Equal("expected", actualException.Message);
            Assert.Equal(0, secondCount);
        }

        [Fact]
        public async Task InvokeAsync_OrderedStopOnFirstErrorTrueSecondAsync_FirstExceptionPreventsExecution()
        {
            var invoker = new GenericEventHandlerInvoker();

            int secondCount = 0;
            invoker.Event += (_, __) =>
            {
                throw new InvalidOperationException("expected");
            };
            invoker.Event += async (_, __) =>
            {
                await Task.Run(() => secondCount++).ConfigureAwait(false);
            };

            var actualException = await Assert
                .ThrowsAsync<InvalidOperationException>(async () => await invoker
                    .InvokeAsync(true, true)
                    .ConfigureAwait(false))
                .ConfigureAwait(false);
            Assert.Equal("expected", actualException.Message);
            Assert.Equal(0, secondCount);
        }

        [Fact]
        public async Task InvokeAsync_UnorderedStopOnFirstErrorTrueBothAsync_FirstExceptionMayPreventsExecution()
        {
            var invoker = new GenericEventHandlerInvoker();

            int secondCount = 0;
            invoker.Event += async (_, __) =>
            {
                await Task.Run(() => throw new InvalidOperationException("expected")).ConfigureAwait(false);
            };
            invoker.Event += async (_, __) =>
            {
                await Task.Run(() => secondCount++).ConfigureAwait(false);
            };

            var actualException = await Assert
                .ThrowsAsync<InvalidOperationException>(async () => await invoker
                    .InvokeAsync(false, true)
                    .ConfigureAwait(false))
                .ConfigureAwait(false);
            Assert.Equal("expected", actualException.Message);
            Assert.InRange(secondCount, 0, 1);
        }

        [Fact]
        public async Task InvokeAsync_UnorderedStopOnFirstErrorTrueFirstAsync_FirstExceptionMayPreventsExecution()
        {
            var invoker = new GenericEventHandlerInvoker();

            int secondCount = 0;
            invoker.Event += async (_, __) =>
            {
                await Task.Run(() => throw new InvalidOperationException("expected")).ConfigureAwait(false);
            };
            invoker.Event += (_, __) =>
            {
                secondCount++;
            };

            var actualException = await Assert
                .ThrowsAsync<InvalidOperationException>(async () => await invoker
                    .InvokeAsync(false, true)
                    .ConfigureAwait(false))
                .ConfigureAwait(false);
            Assert.Equal("expected", actualException.Message);
            Assert.InRange(secondCount, 0, 1);
        }

        [Fact]
        public async Task InvokeAsync_UnorderedStopOnFirstErrorTrueSecondAsync_FirstExceptionMayPreventsExecution()
        {
            var invoker = new GenericEventHandlerInvoker();

            int secondCount = 0;
            invoker.Event += (_, __) =>
            {
                throw new InvalidOperationException("expected");
            };
            invoker.Event += async (_, __) =>
            {
                await Task.Run(() => secondCount++).ConfigureAwait(false);
            };

            var actualException = await Assert
                .ThrowsAsync<InvalidOperationException>(async () => await invoker
                    .InvokeAsync(false, true)
                    .ConfigureAwait(false))
                .ConfigureAwait(false);
            Assert.Equal("expected", actualException.Message);
            Assert.InRange(secondCount, 0, 1);
        }

        private sealed class GenericEventHandlerInvoker
        {
            public event EventHandler<EventArgsA> Event;

            public async Task InvokeAsync(
                bool ordered,
                bool stopOnFirstError)
            {
                await Event
                    .InvokeAsync(
                        this,
                        new EventArgsA(),
                        ordered,
                        stopOnFirstError)
                    .ConfigureAwait(false);
            }
        }

        private sealed class EventHandlerInvoker
        {
            public event EventHandler Event;

            public async Task InvokeAsync(
                bool ordered,
                bool stopOnFirstError)
            {
                await Event
                    .InvokeAsync(
                        this,
                        EventArgs.Empty,
                        ordered,
                        stopOnFirstError)
                    .ConfigureAwait(false);
            }
        }

        private sealed class EventArgsA : EventArgs
        {
        }
    }
}
