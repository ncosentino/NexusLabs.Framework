using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

using Xunit;

namespace NexusLabs.Framework.Tests
{
    [ExcludeFromCodeCoverage]
    public sealed class ActionExtensionTests
    {
        [Fact]
        public async Task InvokeAsync_AsyncVoidActionNoArgsThrows_CanCatch()
        {
            var asyncVoidAction = new Action(async () =>
            {
                await ThrowsExAsyncTask();
            });

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => 
                asyncVoidAction.InvokeAsync());
            Assert.Equal("expected", exception.Message);
        }

        [Fact]
        public async Task InvokeAsync_VoidActionNoArgsThrows_CanCatch()
        {
            var voidAction = ThrowsExVoid;

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                voidAction.InvokeAsync());
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

            await voidAction.InvokeAsync();
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
