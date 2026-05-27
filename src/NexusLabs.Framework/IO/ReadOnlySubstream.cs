using System;
using System.IO;

namespace NexusLabs.Framework.IO;

/// <summary>
/// Provides a read-only window (subsection) over an existing parent <see cref="Stream"/>.
/// The substream exposes only the specified range (offset + length) and prevents writes
/// and length modifications. Seeking and reading are constrained to the declared substream
/// bounds.
/// </summary>
/// <remarks>
/// This type does not copy data; it delegates directly to the underlying parent stream,
/// adjusting the effective position by the configured offset. Optionally, ownership of the
/// parent stream can be taken so that disposing this substream disposes the parent.
/// </remarks>
public sealed class ReadOnlySubstream : Stream
{
    private readonly Stream _stream;
    private readonly long _offsetWithinStream;
    [TransfersOwnership]
    private readonly bool _takeOwnership;
    private long _position;

    /// <summary>
    /// Initializes a new instance of <see cref="ReadOnlySubstream"/> using the full length
    /// of the parent <paramref name="stream"/> to determine bounds.
    /// </summary>
    /// <param name="stream">The parent stream that contains the data region.</param>
    /// <param name="options">
    /// Options describing the offset and length of the substream as well as ownership behavior.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <see cref="SubstreamOptions.Offset"/> or <see cref="SubstreamOptions.Length"/> are negative.
    /// </exception>
    public ReadOnlySubstream(
        Stream stream,
        SubstreamOptions options) :
        this(stream, stream.Length, options)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ReadOnlySubstream"/> with an explicit
    /// parent stream length value (useful if the parent stream <see cref="Stream.Length"/> is
    /// not accessible or should be overridden).
    /// </summary>
    /// <param name="stream">The parent stream that contains the data region.</param>
    /// <param name="parentStreamLength">
    /// The effective length of the parent stream used to clamp the substream length.
    /// </param>
    /// <param name="options">
    /// Options describing the offset and length of the substream as well as ownership behavior.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <see cref="SubstreamOptions.Offset"/> or <see cref="SubstreamOptions.Length"/> are negative.
    /// </exception>
    public ReadOnlySubstream(
        Stream stream,
        long parentStreamLength,
        SubstreamOptions options)
    {
        if (options.Offset < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                $"{nameof(options.Offset)} cannot be less than zero (Was {options.Offset}).");
        }

        if (options.Length < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                $"{nameof(options.Length)} cannot be less than zero (Was {options.Length}).");
        }

        _stream = stream;
        _takeOwnership = options.TakeOwnershipOfStream;
        _offsetWithinStream = options.Offset;
        if (!options.AssumeParentStreamOffsetCorrect)
        {
            _stream.Seek(_offsetWithinStream, SeekOrigin.Begin);
        }

        Length = Math.Max(0, Math.Min(options.Length, parentStreamLength - _offsetWithinStream));
    }

    /// <summary>
    /// Gets a value indicating whether the substream supports reading. Mirrors the parent stream capability.
    /// </summary>
    public override bool CanRead => _stream.CanRead;

    /// <summary>
    /// Gets a value indicating whether the substream supports seeking. Mirrors the parent stream capability.
    /// </summary>
    public override bool CanSeek => _stream.CanSeek;

    /// <summary>
    /// Always returns <c>false</c>; write operations are not supported.
    /// </summary>
    public override bool CanWrite => false;

    /// <summary>
    /// Gets the length (in bytes) of the exposed substream region. This is clamped so it does not
    /// extend beyond the parent stream's effective length minus the configured offset.
    /// </summary>
    public override long Length { get; }

    /// <summary>
    /// Gets or sets the current position (in bytes) within the substream.
    /// Setting this updates the parent stream position to the substream offset plus the provided value.
    /// </summary>
    /// <remarks>
    /// The setter does not clamp arbitrarily large values; external callers should ensure the value
    /// does not exceed <see cref="Length"/> when performing reads.
    /// </remarks>
    public override long Position
    {
        get => _position;
        set
        {
            _position = value;
            _stream.Position = value + _offsetWithinStream;
        }
    }

    /// <summary>
    /// No-op for a read-only substream. Flush is ignored.
    /// </summary>
    public override void Flush()
    {
    }

    /// <summary>
    /// Reads up to <paramref name="count"/> bytes from the substream into <paramref name="buffer"/>,
    /// starting at <paramref name="offset"/> within the destination buffer.
    /// </summary>
    /// <param name="buffer">Destination buffer to populate with read data.</param>
    /// <param name="offset">The zero-based index in <paramref name="buffer"/> at which to begin storing data.</param>
    /// <param name="count">The maximum number of bytes to read.</param>
    /// <returns>
    /// The total number of bytes read into the buffer. Returns 0 if at end of substream or no bytes are available.
    /// </returns>
    /// <remarks>
    /// Reading stops when either the requested count is satisfied, the end of the substream is reached,
    /// or the underlying stream returns 0 indicating no further data.
    /// </remarks>
    public override int Read(byte[] buffer, int offset, int count)
    {
        int remaining = (int)Math.Min(count, Length - Position);
        if (remaining <= 1)
        {
            return 0;
        }

        int totalRead = 0;
        while (Position < Length && remaining > 0)
        {
            var read = _stream.Read(buffer, offset, remaining);
            if (read < 1)
            {
                break;
            }

            totalRead += read;
            remaining -= read;
            offset += read;
            _position += read;
        }

        return totalRead;
    }

    /// <summary>
    /// Sets the substream position relative to the specified <paramref name="origin"/>.
    /// </summary>
    /// <param name="offset">The byte offset relative to <paramref name="origin"/>.</param>
    /// <param name="origin">A value indicating the reference point used to obtain the new position.</param>
    /// <returns>The new position within the substream.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the underlying stream does not support seeking.</exception>
    public override long Seek(long offset, SeekOrigin origin)
    {
        if (!CanSeek)
        {
            throw new InvalidOperationException("The stream does not support seeking.");
        }

        switch (origin)
        {
            case SeekOrigin.Begin:
                Position = Math.Min(offset, Length);
                break;
            case SeekOrigin.Current:
                Position = Math.Min(Position + offset, Length);
                break;
            case SeekOrigin.End:
                Position = Length + offset;
                break;
        }

        return Position;
    }

    /// <summary>
    /// Not supported for a read-only substream.
    /// </summary>
     /// <param name="value">Ignored.</param>
    /// <exception cref="NotSupportedException">Always thrown.</exception>
    public override void SetLength(long value)
        => throw new NotSupportedException();

    /// <summary>
    /// Not supported; this substream is read-only.
    /// </summary>
    /// <param name="buffer">Ignored.</param>
    /// <param name="offset">Ignored.</param>
    /// <param name="count">Ignored.</param>
    /// <exception cref="NotSupportedException">Always thrown.</exception>
    public override void Write(byte[] buffer, int offset, int count)
        => throw new NotSupportedException();

    /// <summary>
    /// Disposes the substream and, if configured via <see cref="SubstreamOptions.TakeOwnershipOfStream"/>,

    /// also disposes the underlying parent stream.
    /// </summary>
    /// <param name="disposing"><c>true</c> to release managed resources.</param>
    protected override void Dispose(bool disposing)
    {
        try
        {
            if (disposing && _takeOwnership)
            {
                _stream.Dispose();
            }
        }
        finally
        {
            base.Dispose(disposing);
        }
    }
}

