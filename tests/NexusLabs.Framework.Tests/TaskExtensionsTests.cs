using System;
using System.Linq;
using System.Threading.Tasks;

using Xunit;

namespace NexusLabs.Framework.Tests
{
    public sealed class TaskExtensionsTests
    {
        [Fact]
        private async Task ToUnorderedAsyncEnumerable_EarlyEnumerationExit_CancelsRemainingTasks()
        {
            const int TARGET_TAKE = 3;
            var source = new int[] { 5, 4, 3, 2, 1 };
            var results = await source
                .ToUnorderedAsyncEnumerable(async (x, cancellationToken) =>
                {
                    if (x <= TARGET_TAKE)
                    {
                        return x;
                    }

                    await Task.Delay(x * 10000, cancellationToken);
                    throw new InvalidOperationException(
                        $"Task should have been cancelled before ever reaching this.");
                })
                .Take(TARGET_TAKE)
                .ToArrayAsync();
            Assert.Equal(
                source.Where(x => x <= TARGET_TAKE).Take(TARGET_TAKE),
                results);
        }

        [Fact]
        private async Task ToOrderedAsyncEnumerable_EarlyEnumerationExit_CancelsRemainingTasks()
        {
            const int TARGET_TAKE = 3;
            var source = new int[] { 1, 2, 3, 4, 5 };
            var results = await source
                .ToOrderedAsyncEnumerable(async (x, cancellationToken) =>
                {
                    if (x <= TARGET_TAKE)
                    {
                        return x;
                    }

                    await Task.Delay(x * 10000, cancellationToken);
                    throw new InvalidOperationException(
                        $"Task should have been cancelled before ever reaching this.");
                })
                .Take(TARGET_TAKE)
                .ToArrayAsync();
            Assert.Equal(
                source.Where(x => x <= TARGET_TAKE).Take(TARGET_TAKE),
                results);
        }
    }
}
