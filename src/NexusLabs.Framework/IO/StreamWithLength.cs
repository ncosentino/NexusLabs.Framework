using System.IO;

namespace NexusLabs.Framework.IO;

/// <summary>
/// Wraps an existing <see cref="Stream"/> while exposing a logical length that
/// can differ from the underlying stream's intrinsic <see cref="Stream.Length"/>.
/// </summary>
/// <remarks>
/// Operations (read, write, seek, flush) are delegated directly to the wrapped stream.
/// The logical length reported via <see cref="Length"/> is initialized from the primary
/// constructor parameter <paramref name="_length"/> and can be changed with
/// <see cref="SetLength(long)"/> without affecting the underlying stream's own length.
/// Setting <see cref="Position"/> is delegated unconditionally to the wrapped stream;
/// use <see cref="Seek(long, SeekOrigin)"/> for explicit repositioning.
/// If <paramref name="_takeOwnershipOfStream"/> is <c>true</c>, disposing this wrapper
/// will also dispose the underlying stream.
/// </remarks>
/// <param name="_streamToWrap">The underlying stream instance to wrap. Must not be null.</param>
/// <param name="_length">
/// The logical length this wrapper will report. It does not validate against the
/// underlying stream's actual length.
/// </param>
/// <param name="_takeOwnershipOfStream">
/// When <c>true</c>, disposing this wrapper also disposes the wrapped stream; otherwise
/// the wrapped stream remains open.
/// </param>
public sealed class StreamWithLength(
    Stream _streamToWrap,
    long _length,
    [TransfersOwnership(nameof(_streamToWrap))] bool _takeOwnershipOfStream) : Stream
{
    /// <summary>
    /// Indicates whether the wrapped stream supports reading.
    /// </summary>
    public override bool CanRead => _streamToWrap.CanRead;

    /// <summary>
    /// Indicates whether the wrapped stream supports seeking.
    /// </summary>
    public override bool CanSeek => _streamToWrap.CanSeek;

    /// <summary>
    /// Indicates whether the wrapped stream supports writing.
    /// </summary>
    public override bool CanWrite => _streamToWrap.CanWrite;

    /// <summary>
    /// Gets the logical length exposed by this wrapper (not necessarily the wrapped stream's actual length).
    /// </summary>
    public override long Length => _length;

    /// <summary>
    /// Gets or sets the current position within the wrapped stream.
    /// Set is unconditionally delegated to the wrapped stream's <see cref="Stream.Position"/>;
    /// callers are responsible for ensuring the value is within the wrapped stream's bounds.
    /// </summary>
    public override long Position
    {
        get => _streamToWrap.Position;
        set => _streamToWrap.Position = value;
    }

    /// <summary>
    /// Flushes any buffered data to the underlying stream.
    /// </summary>
    public override void Flush() => _streamToWrap.Flush();

    /// <summary>
    /// Reads a sequence of bytes from the wrapped stream and advances the position.
    /// </summary>
    /// <param name="buffer">Destination buffer.</param>
    /// <param name="offset">Zero-based index in <paramref name="buffer"/> at which to begin storing data.</param>
    /// <param name="count">Maximum number of bytes to read.</param>
    /// <returns>The total number of bytes read into the buffer.</returns>
    public override int Read(byte[] buffer, int offset, int count) =>
        _streamToWrap.Read(buffer, offset, count);

    /// <summary>
    /// Sets the position within the wrapped stream.
    /// </summary>
    /// <param name="offset">A byte offset relative to the <paramref name="origin"/>.</param>
    /// <param name="origin">Reference point used to obtain the new position.</param>
    /// <returns>The new position within the stream.</returns>
    public override long Seek(long offset, SeekOrigin origin) =>
        _streamToWrap.Seek(offset, origin);

    /// <summary>
    /// Sets the logical length reported by this wrapper (does not modify the underlying stream length).
    /// </summary>
    /// <param name="value">The new logical length.</param>
    public override void SetLength(long value)
    {
        _length = value;
    }

    /// <summary>
    /// Writes a sequence of bytes to the wrapped stream and advances the position.
    /// </summary>
    /// <param name="buffer">Source buffer containing data to write.</param>
    /// <param name="offset">Zero-based index in <paramref name="buffer"/> at which to begin reading data.</param>
    /// <param name="count">Number of bytes to write.</param>
    public override void Write(byte[] buffer, int offset, int count) =>
        _streamToWrap.Write(buffer, offset, count);

    /// <summary>
    /// Disposes the wrapper and, if ownership was taken, the underlying stream.
    /// </summary>
    /// <param name="disposing">
    /// <c>true</c> when called from <see cref="Dispose()"/>; <c>false</c> when called from the finalizer.
    /// </param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && _takeOwnershipOfStream)
        {
            _streamToWrap.Dispose();
        }

        base.Dispose(disposing);
    }
}