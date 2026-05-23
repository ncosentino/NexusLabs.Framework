using System;
using System.Threading.Tasks;

using Xunit;

namespace NexusLabs.Framework.Tests
{
    public sealed class SafelyTests
    {
        [Fact]
        private void GetResultOrFalse_ExceptionThrown_ReturnsFalse()
        {
            var exception = new InvalidOperationException("expected");
            var result = Safely.GetResultOrFalse(() =>
            {
                throw exception;
                return new object();
            });

            Assert.False(result, "Unexpected value for result");
        }

        [Fact]
        private void GetResultOrFalse_InnerFuncExceptionThrown_ReturnsFalse()
        {
            var exception = new InvalidOperationException("expected");
            var result = Safely.GetResultOrFalse(() => 
            {
                return Safely.GetResultOrFalse(() =>
                {
                    throw exception;
                    return new object();
                });
            });

            Assert.False(result, "Unexpected value for result");
        }

        [Fact]
        private void GetResultOrFalse_NoException_ReturnsObject()
        {
            var obj = new object();
            var result = Safely.GetResultOrFalse(() => obj);

            Assert.True(result, "Unexpected value for result");
            Assert.Equal(obj, result.Value);
        }

        [Fact]
        private void GetResultOrFalse_InnerFuncNoException_ReturnsObject()
        {
            var obj = new object();
            var result = Safely.GetResultOrFalse<object>(() =>
            {
                return Safely.GetResultOrFalse(() => obj);
            });

            Assert.True(result, "Unexpected value for result");
            Assert.Equal(obj, result.Value);
        }

        [Fact]
        private void GetResultOrException_ExceptionThrown_ReturnsException()
        {
            var exception = new InvalidOperationException("expected");
            var result = Safely.GetResultOrException(() =>
            {
                throw exception;
                return new object();
            });

            Assert.False(result.Success, "Unexpected value for result's success");
            Assert.Equal(exception, result.Error);
        }

        [Fact]
        private void GetResultOrException_InnerFuncExceptionThrown_ReturnsException()
        {
            var exception = new InvalidOperationException("expected");
            var result = Safely.GetResultOrException(() =>
            {
                return Safely.GetResultOrException(() =>
                {
                    throw exception;
                    return new object();
                });
            });

            Assert.False(result.Success, "Unexpected value for result's success");
            Assert.Equal(exception, result.Error);
        }

        [Fact]
        private void GetResultOrException_NoException_ReturnsObject()
        {
            var obj = new object();
            var result = Safely.GetResultOrException(() => obj);

            Assert.True(result.Success, "Unexpected value for result's success");
            Assert.Throws<InvalidOperationException>(() => result.Error);
            Assert.Equal(obj, result.Value);
        }

        [Fact]
        private void GetResultOrException_InnerFuncNoException_ReturnsObject()
        {
            var obj = new object();
            var result = Safely.GetResultOrException(() => 
            {
                return Safely.GetResultOrException(() => obj);
            });

            Assert.True(result.Success, "Unexpected value for result's success");
            Assert.Throws<InvalidOperationException>(() => result.Error);
            Assert.Equal(obj, result.Value);
        }

        [Fact]
        private async Task GetResultOrFalseAsync_ExceptionThrown_ReturnsFalse()
        {
            var exception = new InvalidOperationException("expected");
            var result = await Safely.GetResultOrFalseAsync(async () =>
            {
                throw exception;
                return new object();
            });

            Assert.False(result, "Unexpected value for result");
        }

        [Fact]
        private async Task GetResultOrFalseAsync_InnerAsyncFuncExceptionThrown_ReturnsFalse()
        {
            var exception = new InvalidOperationException("expected");
            var result = await Safely.GetResultOrFalseAsync(async () =>
            {
                return await Safely.GetResultOrFalseAsync(async () =>
                {
                    throw exception;
                    return new object();
                });
            });

            Assert.False(result, "Unexpected value for result");
        }

        [Fact]
        private async Task GetResultOrFalseAsync_NoException_ReturnsObject()
        {
            var obj = new object();
            var result = await Safely.GetResultOrFalseAsync(async () => obj);

            Assert.True(result, "Unexpected value for result");
            Assert.Equal(obj, result.Value);
        }

        [Fact]
        private async Task GetResultOrFalseAsync_InnerFuncNoException_ReturnsObject()
        {
            var obj = new object();
            var result = await Safely.GetResultOrFalseAsync(async () =>
            {
                return await Safely.GetResultOrFalseAsync(async () => obj);
            });

            Assert.True(result, "Unexpected value for result");
            Assert.Equal(obj, result.Value);
        }

        [Fact]
        private async Task GetResultOrExceptionAsync_ExceptionThrown_ReturnsException()
        {
            var exception = new InvalidOperationException("expected");
            var result = await Safely.GetResultOrExceptionAsync(async () =>
            {
                throw exception;
                return new object();
            });

            Assert.False(result.Success, "Unexpected value for result's success");
            Assert.Equal(exception, result.Error);
        }

        [Fact]
        private async Task GetResultOrExceptionAsync_InnerAsyncExceptionThrown_ReturnsException()
        {
            var exception = new InvalidOperationException("expected");
            var result = await Safely.GetResultOrExceptionAsync(async () =>
            {
                return await Safely.GetResultOrExceptionAsync(async () =>
                {
                    throw exception;
                    return 1;
                });
            });

            Assert.False(result.Success, "Unexpected value for result's success");
            Assert.Equal(exception, result.Error);
        }

        [Fact]
        private async Task GetResultOrExceptionAsync_NoException_ReturnsObject()
        {
            var obj = new object();
            var result = await Safely.GetResultOrExceptionAsync(async () => obj);

            Assert.True(result.Success, "Unexpected value for result's success");
            Assert.Throws<InvalidOperationException>(() => result.Error);
            Assert.Equal(obj, result.Value);
        }

        [Fact]
        private async Task GetResultOrExceptionAsync_InnerAsyncFuncNoException_ReturnsObject()
        {
            var obj = new object();
            var result = await Safely.GetResultOrExceptionAsync(async () => 
            {
                return await Safely.GetResultOrExceptionAsync(async () => obj);
            });

            Assert.True(result.Success, "Unexpected value for result's success");
            Assert.Throws<InvalidOperationException>(() => result.Error);
            Assert.Equal(obj, result.Value);
        }

        [Fact]
        private async Task GetResultOrExceptionAsync_InnerAsyncFuncNoException_DeconstructedThreeParams()
        {
            var obj = new object();
            (var success, var result, var error) = await Safely.GetResultOrExceptionAsync(async () =>
            {
                return await Safely.GetResultOrExceptionAsync(async () => obj);
            });

            Assert.True(success, "Unexpected value for result's success");
            Assert.Equal(obj, result);
            Assert.Null(error);
        }

        [Fact]
        private async Task GetResultOrExceptionAsync_InnerAsyncFuncNoException_MatchSuccess()
        {
            var matchSucess = false;
            var matchFailed = false;
            var obj = new object();
            (await Safely.GetResultOrExceptionAsync(async () =>
            {
                return await Safely.GetResultOrExceptionAsync(async () => obj);
            }))
            .Match(
                o =>
                {
                    matchSucess = true;
                    Assert.Equal(obj, o);
                },
                e =>
                {
                    matchFailed = true;
                });

            Assert.True(matchSucess, "Unexpected value for match success.");
            Assert.False(matchFailed, "Unexpected value for match failed.");
        }
    }
}
