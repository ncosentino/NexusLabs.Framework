using System;
using System.Buffers;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Moq;

using NexusLabs.Framework.Buffers;

using Xunit;

namespace NexusLabs.Framework.Tests.Buffers;

public sealed class RentedMemoryTests
{
    private readonly MockRepository _mockRepository = new(MockBehavior.Strict);

    [Fact]
    public void RentMemory_RentsRequestedLengthFromPool()
    {
        var pool = _mockRepository.Create<ArrayPool<byte>>();
        var rented = new byte[64];
        pool.Setup(p => p.Rent(64)).Returns(rented);
        pool.Setup(p => p.Return(rented, false));

        using (var owner = pool.Object.RentMemory(64))
        {
            Assert.Equal(64, owner.Length);
        }

        pool.Verify(p => p.Rent(64), Times.Once);
        _mockRepository.VerifyAll();
    }

    [Fact]
    public void RentMemory_DisposingReturnsArrayToPool()
    {
        var pool = _mockRepository.Create<ArrayPool<byte>>();
        var rented = new byte[16];
        pool.Setup(p => p.Rent(16)).Returns(rented);
        pool.Setup(p => p.Return(rented, false));

        using (pool.Object.RentMemory(16))
        {
        }

        pool.Verify(p => p.Return(rented, false), Times.Once);
        _mockRepository.VerifyAll();
    }

    [Fact]
    public void Copy_DisposingBothReferences_ReturnsArrayExactlyOnce()
    {
        var pool = _mockRepository.Create<ArrayPool<byte>>();
        var rented = new byte[16];
        pool.Setup(p => p.Rent(16)).Returns(rented);
        pool.Setup(p => p.Return(rented, false));

        var owner = pool.Object.RentMemory(16);
        var copy = owner;

        copy.Dispose();
        owner.Dispose();

        pool.Verify(p => p.Return(rented, false), Times.Once);
        _mockRepository.VerifyAll();
    }

    [Fact]
    public void Dispose_CalledTwiceOnSameInstance_ReturnsArrayOnce()
    {
        var pool = _mockRepository.Create<ArrayPool<byte>>();
        var rented = new byte[8];
        pool.Setup(p => p.Rent(8)).Returns(rented);
        pool.Setup(p => p.Return(rented, false));

        var owner = pool.Object.RentMemory(8);
        owner.Dispose();
        owner.Dispose();

        pool.Verify(p => p.Return(rented, false), Times.Once);
        _mockRepository.VerifyAll();
    }

    [Fact]
    public async Task Dispose_ConcurrentAcrossReferences_ReturnsArrayOnce()
    {
        var pool = _mockRepository.Create<ArrayPool<byte>>();
        var rented = new byte[8];
        pool.Setup(p => p.Rent(8)).Returns(rented);
        pool.Setup(p => p.Return(rented, false));

        var owner = pool.Object.RentMemory(8);

        var start = new ManualResetEventSlim(initialState: false);
        var tasks = Enumerable.Range(0, 64).Select(_ => Task.Run(() =>
        {
            start.Wait();
            owner.Dispose();
        })).ToArray();

        start.Set();
        await Task.WhenAll(tasks);

        pool.Verify(p => p.Return(rented, false), Times.Once);
        _mockRepository.VerifyAll();
    }

    [Fact]
    public void RentMemory_ClearOnReturnTrue_ReturnsWithClearFlagSet()
    {
        var pool = _mockRepository.Create<ArrayPool<byte>>();
        var rented = new byte[16];
        pool.Setup(p => p.Rent(16)).Returns(rented);
        pool.Setup(p => p.Return(rented, true));

        using (pool.Object.RentMemory(16, clearOnReturn: true))
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

        using (var owner = pool.Object.RentMemory(40))
        {
            Assert.Equal(40, owner.Length);
            Assert.Equal(128, owner.Capacity);
        }

        _mockRepository.VerifyAll();
    }

    [Fact]
    public void Array_Memory_Span_ShareTheSameBuffer()
    {
        using var owner = ArrayPool<byte>.Shared.RentMemory(8);

        owner.Array[0] = 200;

        Assert.Equal((byte)200, owner.Span[0]);
        Assert.Equal((byte)200, owner.Memory.Span[0]);
    }

    [Fact]
    public void AsArraySegment_ReturnsOffsetZeroCountLengthOverRentedArray()
    {
        using var owner = ArrayPool<byte>.Shared.RentMemory(10);
        owner[0] = 42;

        var segment = owner.AsArraySegment();

        Assert.Same(owner.Array, segment.Array);
        Assert.Equal(0, segment.Offset);
        Assert.Equal(10, segment.Count);
        Assert.Equal((byte)42, segment.Array![0]);
    }

    [Fact]
    public void AsMemory_ReturnsRequestedWindow()
    {
        using var owner = ArrayPool<byte>.Shared.RentMemory(10);
        owner.Span[4] = 77;

        var window = owner.AsMemory(4, 3);

        Assert.Equal(3, window.Length);
        Assert.Equal((byte)77, window.Span[0]);
    }

    [Fact]
    public void Indexer_ReadsAndWritesInPlace()
    {
        using var owner = ArrayPool<byte>.Shared.RentMemory(4);

        owner[0] = 9;

        Assert.Equal((byte)9, owner[0]);
        Assert.Equal((byte)9, owner.Span[0]);
    }

    [Fact]
    public void ExposesMemoryThroughIMemoryOwnerInterface()
    {
        IMemoryOwner<byte> owner = ArrayPool<byte>.Shared.RentMemory(8);

        Assert.Equal(8, owner.Memory.Length);

        owner.Dispose();
    }

    [Fact]
    public void Members_AfterDispose_ThrowObjectDisposedException()
    {
        var owner = ArrayPool<byte>.Shared.RentMemory(16);
        owner.Dispose();

        Assert.Throws<ObjectDisposedException>(() => owner.Array);
        Assert.Throws<ObjectDisposedException>(() => owner.Capacity);
        Assert.Throws<ObjectDisposedException>(() => owner.Memory);
        Assert.Throws<ObjectDisposedException>(() => owner.Span.Length);
    }

    [Fact]
    public void Length_RemainsReadableAfterDispose()
    {
        var owner = ArrayPool<byte>.Shared.RentMemory(24);
        owner.Dispose();

        Assert.Equal(24, owner.Length);
    }

    [Fact]
    public void RentMemory_IsGeneric_WorksForReferenceElementType()
    {
        var pool = _mockRepository.Create<ArrayPool<string>>();
        var rented = new string[4];
        pool.Setup(p => p.Rent(4)).Returns(rented);
        pool.Setup(p => p.Return(rented, false));

        using (var owner = pool.Object.RentMemory(4))
        {
            Assert.Equal(4, owner.Length);
            Assert.Same(rented, owner.Array);
        }

        _mockRepository.VerifyAll();
    }

    [Fact]
    public void RentMemory_NullPool_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => ((ArrayPool<byte>)null!).RentMemory(4));
    }

    [Fact]
    public void RentMemory_NegativeLength_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => ArrayPool<byte>.Shared.RentMemory(-1));
    }
}
