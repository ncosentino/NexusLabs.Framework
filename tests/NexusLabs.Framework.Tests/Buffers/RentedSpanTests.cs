using System;
using System.Buffers;

using Moq;

using NexusLabs.Framework.Buffers;

using Xunit;

namespace NexusLabs.Framework.Tests.Buffers;

public sealed class RentedSpanTests
{
    private readonly MockRepository _mockRepository = new(MockBehavior.Strict);

    [Fact]
    public void RentSpan_RentsRequestedLengthFromPool()
    {
        var pool = _mockRepository.Create<ArrayPool<byte>>();
        var rented = new byte[64];
        pool.Setup(p => p.Rent(64)).Returns(rented);
        pool.Setup(p => p.Return(rented, false));

        using (var buffer = pool.Object.RentSpan(64))
        {
            Assert.Equal(64, buffer.Length);
        }

        pool.Verify(p => p.Rent(64), Times.Once);
        _mockRepository.VerifyAll();
    }

    [Fact]
    public void RentSpan_DisposingReturnsRentedArrayToPool()
    {
        var pool = _mockRepository.Create<ArrayPool<byte>>();
        var rented = new byte[16];
        pool.Setup(p => p.Rent(16)).Returns(rented);
        pool.Setup(p => p.Return(rented, false));

        using (pool.Object.RentSpan(16))
        {
        }

        pool.Verify(p => p.Return(rented, false), Times.Once);
        _mockRepository.VerifyAll();
    }

    [Fact]
    public void RentSpan_ClearOnReturnTrue_ReturnsWithClearFlagSet()
    {
        var pool = _mockRepository.Create<ArrayPool<byte>>();
        var rented = new byte[16];
        pool.Setup(p => p.Rent(16)).Returns(rented);
        pool.Setup(p => p.Return(rented, true));

        using (pool.Object.RentSpan(16, clearOnReturn: true))
        {
        }

        pool.Verify(p => p.Return(rented, true), Times.Once);
        _mockRepository.VerifyAll();
    }

    [Fact]
    public void Capacity_ReflectsActualArrayLength_DistinctFromRequestedLength()
    {
        var pool = _mockRepository.Create<ArrayPool<byte>>();
        var rented = new byte[128];
        pool.Setup(p => p.Rent(40)).Returns(rented);
        pool.Setup(p => p.Return(rented, false));

        using (var buffer = pool.Object.RentSpan(40))
        {
            Assert.Equal(40, buffer.Length);
            Assert.Equal(128, buffer.Capacity);
        }

        _mockRepository.VerifyAll();
    }

    [Fact]
    public void Dispose_CalledTwiceOnSameInstance_ReturnsArrayOnce()
    {
        var pool = _mockRepository.Create<ArrayPool<byte>>();
        var rented = new byte[8];
        pool.Setup(p => p.Rent(8)).Returns(rented);
        pool.Setup(p => p.Return(rented, false));

        var buffer = pool.Object.RentSpan(8);
        buffer.Dispose();
        buffer.Dispose();

        pool.Verify(p => p.Return(rented, false), Times.Once);
        _mockRepository.VerifyAll();
    }

    [Fact]
    public void Length_EqualsRequestedLength()
    {
        using var buffer = ArrayPool<byte>.Shared.RentSpan(48);

        Assert.Equal(48, buffer.Length);
        Assert.Equal(48, buffer.Span.Length);
    }

    [Fact]
    public void Array_ExposesRentedBuffer_SharedWithSpanView()
    {
        using var buffer = ArrayPool<byte>.Shared.RentSpan(8);

        buffer.Array[0] = 200;

        Assert.Equal((byte)200, buffer.Span[0]);
    }

    [Fact]
    public void AsSpan_ReturnsRequestedWindowOverRentedArray()
    {
        using var buffer = ArrayPool<byte>.Shared.RentSpan(10);
        buffer.Span[4] = 77;

        var window = buffer.AsSpan(4, 3);

        Assert.Equal(3, window.Length);
        Assert.Equal((byte)77, window[0]);
    }

    [Fact]
    public void Indexer_ReadsAndWritesInPlace()
    {
        using var buffer = ArrayPool<byte>.Shared.RentSpan(4);

        buffer[0] = 9;
        buffer[1] = 8;

        Assert.Equal((byte)9, buffer[0]);
        Assert.Equal((byte)8, buffer[1]);
        Assert.Equal((byte)9, buffer.Span[0]);
    }

    [Fact]
    public void Members_AfterDispose_ThrowObjectDisposedException()
    {
        Assert.Throws<ObjectDisposedException>(() =>
        {
            var buffer = ArrayPool<byte>.Shared.RentSpan(16);
            buffer.Dispose();
            _ = buffer.Array;
        });

        Assert.Throws<ObjectDisposedException>(() =>
        {
            var buffer = ArrayPool<byte>.Shared.RentSpan(16);
            buffer.Dispose();
            _ = buffer.Capacity;
        });

        Assert.Throws<ObjectDisposedException>(() =>
        {
            var buffer = ArrayPool<byte>.Shared.RentSpan(16);
            buffer.Dispose();
            _ = buffer.AsSpan(0, 1).Length;
        });
    }

    [Fact]
    public void RentSpan_SynchronousRentUseReturn_AllocatesZeroBytes()
    {
        var pool = ArrayPool<byte>.Shared;

        for (var warmup = 0; warmup < 10_000; warmup++)
        {
            using var warmupBuffer = pool.RentSpan(256);
            warmupBuffer.Span[0] = 1;
        }

        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        var sink = 0;
        for (var i = 0; i < 10_000; i++)
        {
            using var buffer = pool.RentSpan(256);
            buffer.Span[0] = 1;
            sink += buffer.Span[0];
        }
        var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

        Assert.NotEqual(0, sink);
        Assert.Equal(0, allocatedBytes);
    }

    [Fact]
    public void RentSpan_NullPool_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () =>
            {
                using var buffer = ((ArrayPool<byte>)null!).RentSpan(4);
            });
    }

    [Fact]
    public void RentSpan_NegativeLength_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
            {
                using var buffer = ArrayPool<byte>.Shared.RentSpan(-1);
            });
    }
}
