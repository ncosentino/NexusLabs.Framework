using System;
using System.Buffers;

namespace NexusLabs.Framework.Buffers;

/// <summary>
/// A strictly-scoped, synchronous handle over an array rented from an <see cref="ArrayPool{T}"/>.
/// This is the zero-allocation <c>ref struct</c> companion to <see cref="RentedMemory{T}"/>: obtain
/// one with the
/// <see cref="ArrayPoolExtensions.RentSpan{T}(ArrayPool{T}, int, bool)"/> extension and bind the
/// return-to-pool to scope exit with <c>using</c>:
/// <code>
/// using var buffer = ArrayPool&lt;byte&gt;.Shared.RentSpan(capacity);
/// var read = stream.Read(buffer.Span);
/// Process(buffer.AsSpan(0, read));
/// // the array is returned to the pool when the scope exits, on every path
/// </code>
/// </summary>
/// <remarks>
/// <para>
/// Being a <c>ref struct</c>, the compiler guarantees this handle can never escape to the heap: it
/// cannot be boxed, stored in a field of a class, captured by a lambda, placed in a collection, or
/// held across <c>await</c>/<c>yield</c>. That makes the accidental "use the buffer after it was
/// returned" class of bugs impossible. The trade-off is that it is <strong>synchronous only</strong>
/// — if you need to hold a rented buffer across <c>await</c>, use <see cref="RentedMemory{T}"/>, the
/// reference-type owner that additionally exposes <c>Memory</c>.
/// </para>
/// <para>
/// The compiler blocks every heap escape, but it does <em>not</em> stop a plain stack copy
/// (<c>var b = a;</c>) or a by-value argument pass — copying still creates a second owner of the
/// same rented array, so disposing both copies returns it twice. Bind the handle to a single
/// <c>using</c> and use it in place; pass the <em>view</em> (<see cref="Span"/>, <see cref="Array"/>)
/// to other methods rather than the handle. The NLF0024 analyzer flags the copy patterns the
/// compiler allows. <see cref="Dispose"/> on a single instance is idempotent.
/// </para>
/// <para>
/// After disposal the rented array is no longer owned by this handle; accessing <see cref="Array"/>,
/// <see cref="Capacity"/>, <see cref="Span"/>, the indexer, or <see cref="AsSpan(int, int)"/> throws
/// <see cref="ObjectDisposedException"/>. <see cref="Length"/> remains readable because it is the
/// length you requested, not a view over the (returned) buffer.
/// </para>
/// </remarks>
/// <typeparam name="T">The element type of the rented array.</typeparam>
public ref struct RentedSpan<T> : IDisposable
{
    private ArrayPool<T>? _pool;
    private T[]? _array;
    private readonly bool _clearOnReturn;

    /// <summary>
    /// Rents an array of at least <paramref name="minimumLength"/> elements from
    /// <paramref name="pool"/>. Prefer the
    /// <see cref="ArrayPoolExtensions.RentSpan{T}(ArrayPool{T}, int, bool)"/> extension over calling
    /// this directly.
    /// </summary>
    /// <param name="pool">The pool to rent from and return to on disposal.</param>
    /// <param name="minimumLength">
    /// The minimum number of elements required. The pool may hand back a larger array; see
    /// <see cref="Capacity"/>.
    /// </param>
    /// <param name="clearOnReturn">
    /// Whether the array is cleared when returned to the pool. Set <see langword="true"/> when the
    /// buffer held sensitive data that must not linger in a pooled array handed to the next renter.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="pool"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="minimumLength"/> is negative.</exception>
    internal RentedSpan(ArrayPool<T> pool, int minimumLength, bool clearOnReturn)
    {
        ArgumentNullException.ThrowIfNull(pool);
        ArgumentOutOfRangeException.ThrowIfNegative(minimumLength);

        _pool = pool;
        _array = pool.Rent(minimumLength);
        Length = minimumLength;
        _clearOnReturn = clearOnReturn;
    }

    /// <summary>
    /// The number of elements requested when renting. This is the length of the <see cref="Span"/>
    /// view, and is always less than or equal to <see cref="Capacity"/>. Remains readable after
    /// disposal.
    /// </summary>
    public readonly int Length { get; }

    /// <summary>
    /// The actual length of the underlying rented array, which the pool may have sized larger than
    /// <see cref="Length"/>.
    /// </summary>
    /// <exception cref="ObjectDisposedException">The handle has been disposed.</exception>
    public readonly int Capacity => GetArrayOrThrow().Length;

    /// <summary>
    /// The raw rented array, which may be larger than <see cref="Length"/>. Use this only to
    /// interoperate with APIs that require a <typeparamref name="T"/><c>[]</c> plus an explicit
    /// length/count; prefer <see cref="Span"/> otherwise. Do not retain a reference to it beyond
    /// this handle's scope — it returns to the pool on disposal.
    /// </summary>
    /// <exception cref="ObjectDisposedException">The handle has been disposed.</exception>
    public readonly T[] Array => GetArrayOrThrow();

    /// <summary>
    /// A <see cref="Span{T}"/> over the first <see cref="Length"/> elements of the rented array.
    /// </summary>
    /// <exception cref="ObjectDisposedException">The handle has been disposed.</exception>
    public readonly Span<T> Span => GetArrayOrThrow().AsSpan(0, Length);

    /// <summary>
    /// A reference to the element at <paramref name="index"/> within the rented array, assignable in
    /// place (for example <c>buffer[0] = value</c>).
    /// </summary>
    /// <param name="index">The zero-based element index.</param>
    /// <exception cref="ObjectDisposedException">The handle has been disposed.</exception>
    /// <exception cref="IndexOutOfRangeException"><paramref name="index"/> is outside the array bounds.</exception>
    public readonly ref T this[int index] => ref GetArrayOrThrow()[index];

    /// <summary>
    /// Creates a <see cref="Span{T}"/> window over the rented array starting at
    /// <paramref name="start"/> for <paramref name="length"/> elements.
    /// </summary>
    /// <param name="start">The zero-based start index within the rented array.</param>
    /// <param name="length">The number of elements in the window.</param>
    /// <exception cref="ObjectDisposedException">The handle has been disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The requested window falls outside the bounds of the rented array.
    /// </exception>
    public readonly Span<T> AsSpan(int start, int length) => GetArrayOrThrow().AsSpan(start, length);

    /// <summary>
    /// Returns the rented array to the pool. Idempotent on a single instance: a second call is a
    /// no-op.
    /// </summary>
    public void Dispose()
    {
        var array = _array;
        if (array is null)
        {
            return;
        }

        _array = null;
        var pool = _pool;
        _pool = null;
        pool!.Return(array, _clearOnReturn);
    }

    private readonly T[] GetArrayOrThrow() =>
        _array ?? throw new ObjectDisposedException(nameof(RentedSpan<T>));
}
