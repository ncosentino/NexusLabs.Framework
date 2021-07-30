using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

using Xunit;

namespace NexusLabs.Framework.Tests
{
    [ExcludeFromCodeCoverage]
    public sealed class EventHandlerExtensionTests
    {
        [Fact]
        public async Task InvokeAsync_OrderedStopOnFirstErrorTrueBothAsync_ExecutedInOrder()
        {
            var invoker = new Invoker();

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
            var invoker = new Invoker();

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
            var invoker = new Invoker();

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
            var invoker = new Invoker();

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
            var invoker = new Invoker();

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
            var invoker = new Invoker();

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
            var invoker = new Invoker();

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
            var invoker = new Invoker();

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
            var invoker = new Invoker();

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
        public async Task InvokeAsync_UnorderedStopOnFirstErrorFalseBothAsync_FirstExceptionAllowsExecution()
        {
            var invoker = new Invoker();

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
            var invoker = new Invoker();

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
            var invoker = new Invoker();

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
            var invoker = new Invoker();

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
            var invoker = new Invoker();

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
            var invoker = new Invoker();

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
            var invoker = new Invoker();

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
            var invoker = new Invoker();

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
            var invoker = new Invoker();

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

        private sealed class Invoker
        {
            public event EventHandler<EventArgs> Event;

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
    }
}
