using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

using Xunit;

namespace NexusLabs.Framework.Tests
{
    [ExcludeFromCodeCoverage]
    public sealed class AsyncVoidHelperTests
    {
        [Fact]
        public async Task InvokeAsync_AsyncVoidNoArgsThrows_CanCatch()
        {
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await AsyncVoidHelper.InvokeAsync(ThrowsExAsyncVoid));
            Assert.Equal("expected", exception.Message);
        }

        [Fact]
        public async Task InvokeAsync_VoidNoArgsThrows_CanCatch()
        {
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await AsyncVoidHelper.InvokeAsync(ThrowsExVoid));
            Assert.Equal("expected", exception.Message);
        }

        [Fact]
        public async Task InvokeAsync_AsyncVoidActionNoArgsThrows_CanCatch()
        {
            var asyncVoidAction = new Action(async () =>
            {
                await ThrowsExAsyncTask();
            });

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await AsyncVoidHelper.InvokeAsync(asyncVoidAction));
            Assert.Equal("expected", exception.Message);
        }

        [Fact]
        public async Task InvokeAsync_VoidActionNoArgsThrows_CanCatch()
        {
            var voidAction = ThrowsExVoid;

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await AsyncVoidHelper.InvokeAsync(voidAction));
            Assert.Equal("expected", exception.Message);
        }

        [Fact]
        public async Task InvokeAsync_VoidActionNoArgs_ExecutesSuccessfully()
        {
            var actionCount = 0;
            Action voidAction = async () =>
            {
                await Task.Run(() => actionCount++);
            };

            await AsyncVoidHelper.InvokeAsync(voidAction);
            Assert.Equal(1, actionCount);
        }

        private void ThrowsExVoid()
        {
            var expectedException = new InvalidOperationException("expected");
            throw expectedException;
        }

        private async void ThrowsExAsyncVoid()
        {
            await ThrowsExAsyncTask();
        }

        private async Task ThrowsExAsyncTask()
        {
            ThrowsExVoid();
        }
    }
}
