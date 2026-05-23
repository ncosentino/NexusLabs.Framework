using NexusLabs.Framework.IO;

using System;
using System.IO;

using Xunit;

namespace NexusLabs.Framework.Tests.IO;

public sealed class StreamWithLengthTests
{
    [Fact]
    private void Constructor_WithValidParameters_CreatesInstance()
    {
        using var innerStream = new MemoryStream();
        using var wrapper = new StreamWithLength(innerStream, 100, false);

        Assert.NotNull(wrapper);
    }

    [Fact]
    private void Length_ReturnsLogicalLength_NotUnderlyingStreamLength()
    {
        using var innerStream = new MemoryStream(new byte[50]);
        using var wrapper = new StreamWithLength(innerStream, 100, false);

        Assert.Equal(100, wrapper.Length);
        Assert.Equal(50, innerStream.Length);
    }

    [Fact]
    private void Length_WhenSetToZero_ReturnsZero()
    {
        using var innerStream = new MemoryStream(new byte[50]);
        using var wrapper = new StreamWithLength(innerStream, 0, false);

        Assert.Equal(0, wrapper.Length);
    }

    [Fact]
    private void SetLength_UpdatesLogicalLength()
    {
        using var innerStream = new MemoryStream();
        using var wrapper = new StreamWithLength(innerStream, 100, false);

        wrapper.SetLength(200);

        Assert.Equal(200, wrapper.Length);
    }

    [Fact]
    private void SetLength_DoesNotAffectUnderlyingStream()
    {
        using var innerStream = new MemoryStream(new byte[50]);
        var originalLength = innerStream.Length;
        using var wrapper = new StreamWithLength(innerStream, 100, false);

        wrapper.SetLength(200);

        Assert.Equal(originalLength, innerStream.Length);
    }

    [Fact]
    private void CanRead_DelegatesToUnderlyingStream()
    {
        using var readableStream = new MemoryStream();
        using var wrapper = new StreamWithLength(readableStream, 100, false);

        Assert.True(wrapper.CanRead);
    }

    [Fact]
    private void CanSeek_DelegatesToUnderlyingStream()
    {
        using var seekableStream = new MemoryStream();
        using var wrapper = new StreamWithLength(seekableStream, 100, false);

        Assert.True(wrapper.CanSeek);
    }

    [Fact]
    private void CanWrite_DelegatesToUnderlyingStream()
    {
        using var writableStream = new MemoryStream();
        using var wrapper = new StreamWithLength(writableStream, 100, false);

        Assert.True(wrapper.CanWrite);
    }

    [Fact]
    private void CanWrite_WhenUnderlyingStreamIsReadOnly_ReturnsFalse()
    {
        using var innerStream = new MemoryStream(new byte[10], writable: false);
        using var wrapper = new StreamWithLength(innerStream, 100, false);

        Assert.False(wrapper.CanWrite);
    }

    [Fact]
    private void Position_Get_ReturnsUnderlyingStreamPosition()
    {
        using var innerStream = new MemoryStream(new byte[50]);
        innerStream.Position = 25;
        using var wrapper = new StreamWithLength(innerStream, 100, false);

        Assert.Equal(25, wrapper.Position);
    }

    [Fact]
    private void Position_Set_UpdatesUnderlyingStreamPosition()
    {
        using var innerStream = new MemoryStream(new byte[50]);
        using var wrapper = new StreamWithLength(innerStream, 100, false);

        wrapper.Position = 30;

        Assert.Equal(30, innerStream.Position);
        Assert.Equal(30, wrapper.Position);
    }

    [Fact]
    private void Flush_DelegatesToUnderlyingStream()
    {
        using var innerStream = new MemoryStream();
        innerStream.Write(new byte[] { 1, 2, 3 }, 0, 3);
        using var wrapper = new StreamWithLength(innerStream, 100, false);

        // Should not throw
        wrapper.Flush();
    }

    [Fact]
    private void Read_ReadsFromUnderlyingStream()
    {
        var data = new byte[] { 1, 2, 3, 4, 5 };
        using var innerStream = new MemoryStream(data);
        using var wrapper = new StreamWithLength(innerStream, 100, false);

        var buffer = new byte[5];
        var bytesRead = wrapper.Read(buffer, 0, 5);

        Assert.Equal(5, bytesRead);
        Assert.Equal(data, buffer);
    }

    [Fact]
    private void Read_WithOffsetAndCount_ReadsCorrectly()
    {
        var data = new byte[] { 1, 2, 3, 4, 5 };
        using var innerStream = new MemoryStream(data);
        using var wrapper = new StreamWithLength(innerStream, 100, false);

        var buffer = new byte[10];
        var bytesRead = wrapper.Read(buffer, 2, 3);

        Assert.Equal(3, bytesRead);
        Assert.Equal(0, buffer[0]);
        Assert.Equal(0, buffer[1]);
        Assert.Equal(1, buffer[2]);
        Assert.Equal(2, buffer[3]);
        Assert.Equal(3, buffer[4]);
    }

    [Fact]
    private void Read_AdvancesPosition()
    {
        var data = new byte[] { 1, 2, 3, 4, 5 };
        using var innerStream = new MemoryStream(data);
        using var wrapper = new StreamWithLength(innerStream, 100, false);

        var buffer = new byte[3];
#pragma warning disable CA2022 // Avoid inexact read with 'Stream.Read'
        wrapper.Read(buffer, 0, 3);
#pragma warning restore CA2022 // Avoid inexact read with 'Stream.Read'

        Assert.Equal(3, wrapper.Position);
    }

    [Fact]
    private void Write_WritesToUnderlyingStream()
    {
        using var innerStream = new MemoryStream();
        using var wrapper = new StreamWithLength(innerStream, 100, false);

        var data = new byte[] { 1, 2, 3, 4, 5 };
        wrapper.Write(data, 0, 5);

        innerStream.Position = 0;
        var result = new byte[5];
        innerStream.Read(result, 0, 5);

        Assert.Equal(data, result);
    }

    [Fact]
    private void Write_WithOffsetAndCount_WritesCorrectly()
    {
        using var innerStream = new MemoryStream();
        using var wrapper = new StreamWithLength(innerStream, 100, false);

        var data = new byte[] { 0, 0, 1, 2, 3, 0, 0 };
        wrapper.Write(data, 2, 3);

        innerStream.Position = 0;
        var result = new byte[3];
        innerStream.Read(result, 0, 3);

        Assert.Equal(new byte[] { 1, 2, 3 }, result);
    }

    [Fact]
    private void Write_AdvancesPosition()
    {
        using var innerStream = new MemoryStream();
        using var wrapper = new StreamWithLength(innerStream, 100, false);

        var data = new byte[] { 1, 2, 3 };
        wrapper.Write(data, 0, 3);

        Assert.Equal(3, wrapper.Position);
    }

    [Fact]
    private void Seek_FromBegin_SetsCorrectPosition()
    {
        using var innerStream = new MemoryStream(new byte[50]);
        using var wrapper = new StreamWithLength(innerStream, 100, false);

        var newPosition = wrapper.Seek(10, SeekOrigin.Begin);

        Assert.Equal(10, newPosition);
        Assert.Equal(10, wrapper.Position);
    }

    [Fact]
    private void Seek_FromCurrent_SetsCorrectPosition()
    {
        using var innerStream = new MemoryStream(new byte[50]);
        innerStream.Position = 20;
        using var wrapper = new StreamWithLength(innerStream, 100, false);

        var newPosition = wrapper.Seek(5, SeekOrigin.Current);

        Assert.Equal(25, newPosition);
        Assert.Equal(25, wrapper.Position);
    }

    [Fact]
    private void Seek_FromEnd_SetsCorrectPosition()
    {
        using var innerStream = new MemoryStream(new byte[50]);
        using var wrapper = new StreamWithLength(innerStream, 100, false);

        var newPosition = wrapper.Seek(-10, SeekOrigin.End);

        Assert.Equal(40, newPosition);
        Assert.Equal(40, wrapper.Position);
    }

    [Fact]
    private void Dispose_WithOwnership_DisposesUnderlyingStream()
    {
        var innerStream = new MemoryStream();
        var wrapper = new StreamWithLength(innerStream, 100, true);

        wrapper.Dispose();

        Assert.Throws<ObjectDisposedException>(() => innerStream.ReadByte());
    }

    [Fact]
    private void Dispose_WithoutOwnership_DoesNotDisposeUnderlyingStream()
    {
        using var innerStream = new MemoryStream(new byte[] { 1, 2, 3 });
        var wrapper = new StreamWithLength(innerStream, 100, false);

        wrapper.Dispose();

        // Should not throw - stream is still usable
        innerStream.Position = 0;
        Assert.Equal(1, innerStream.ReadByte());
    }

    [Fact]
    private void Dispose_CalledMultipleTimes_DoesNotThrow()
    {
        using var innerStream = new MemoryStream();
        var wrapper = new StreamWithLength(innerStream, 100, false);

        wrapper.Dispose();
        wrapper.Dispose();
        wrapper.Dispose();
    }

    [Fact]
    private void LogicalLength_CanBeLargerThanUnderlyingStream()
    {
        using var innerStream = new MemoryStream(new byte[10]);
        using var wrapper = new StreamWithLength(innerStream, 1000, false);

        Assert.Equal(1000, wrapper.Length);
        Assert.Equal(10, innerStream.Length);
    }

    [Fact]
    private void LogicalLength_CanBeSmallerThanUnderlyingStream()
    {
        using var innerStream = new MemoryStream(new byte[1000]);
        using var wrapper = new StreamWithLength(innerStream, 10, false);

        Assert.Equal(10, wrapper.Length);
        Assert.Equal(1000, innerStream.Length);
    }

    [Fact]
    private void ReadWrite_RoundTrip_PreservesData()
    {
        using var innerStream = new MemoryStream();
        using var wrapper = new StreamWithLength(innerStream, 100, false);

        var originalData = new byte[] { 10, 20, 30, 40, 50 };
        wrapper.Write(originalData, 0, originalData.Length);

        wrapper.Position = 0;

        var readBuffer = new byte[5];
        var bytesRead = wrapper.Read(readBuffer, 0, 5);

        Assert.Equal(5, bytesRead);
        Assert.Equal(originalData, readBuffer);
    }

    [Fact]
    private void Position_SetToZero_Works()
    {
        using var innerStream = new MemoryStream(new byte[50]);
        innerStream.Position = 25;
        using var wrapper = new StreamWithLength(innerStream, 100, false);

        wrapper.Position = 0;

        Assert.Equal(0, wrapper.Position);
        Assert.Equal(0, innerStream.Position);
    }

    [Fact]
    private void SetLength_ToNegativeValue_UpdatesLength()
    {
        using var innerStream = new MemoryStream();
        using var wrapper = new StreamWithLength(innerStream, 100, false);

        // The class allows setting negative length (no validation)
        wrapper.SetLength(-1);

        Assert.Equal(-1, wrapper.Length);
    }
}
