using System;
using System.Buffers;
using System.Threading;

namespace NexusLabs.Framework.Buffers;

/// <summary>
/// A scope-bound, heap-allocated owner of an array rented from an <see cref="ArrayPool{T}"/>,
/// for buffers that must be held across <c>await</c> or otherwise outlive a single stack frame.
/// Obtain one with the <see cref="ArrayPoolExtensions.RentMemory{T}(ArrayPool{T}, int, bool)"/>
/// extension and bind the return-to-pool to scope exit with <c>await using</c> / <c>using</c>:
/// <code>
/// using var buffer = ArrayPool&lt;byte&gt;.Shared.RentMemory(capacity);
/// var read = await stream.ReadAsync(buffer.Memory, cancellationToken).ConfigureAwait(false);
/// Process(buffer.AsMemory(0, read));
/// // the array is returned to the pool when the scope exits, on every path
/// </code>
/// </summary>
/// <remarks>
/// <para>
/// This is a <see langword="class"/> — a reference type — on purpose. Unlike a value-type handle,
/// assigning it, passing it as an argument, or capturing it copies the <em>reference</em>, so every
/// copy points at the same owner and the same rented array. There is exactly one owner no matter how
/// many references exist, so returning the array to the pool exactly once is guaranteed by
/// construction: the idempotent <see cref="Dispose"/> is safe to call through any reference and any
/// number of times. This is the safe counterpart to <see cref="RentedSpan{T}"/> for code that cannot
/// be confined to a single stack frame.
/// </para>
/// <para>
/// The trade-off for that safety is a single small heap allocation for the owner object itself
/// (the rented array — the large allocation — is still pooled). Choose this type when you need to
/// hold a buffer across <c>await</c>; choose <see cref="RentedSpan{T}"/> for synchronous code, where
/// the owner allocation is avoided entirely and the compiler additionally guarantees the buffer
/// cannot escape.
/// </para>
/// <para>
/// After disposal the rented array is no longer owned; accessing <see cref="Array"/>,
/// <see cref="Capacity"/>, <see cref="Memory"/>, <see cref="Span"/>, the indexer, or the
/// <see cref="AsMemory(int, int)"/>/<see cref="AsSpan(int, int)"/>/<see cref="AsArraySegment"/>
/// views throws <see cref="ObjectDisposedException"/>. <see cref="Length"/> remains readable because
/// it is the length you requested, not a view over the (returned) buffer. Do not retain the raw
/// <see cref="Array"/>, <see cref="Memory"/>, or <see cref="Span"/> beyond this owner's scope — they
/// alias a buffer that returns to the pool on disposal.
/// </para>
/// </remarks>
/// <typeparam name="T">The element type of the rented array.</typeparam>
public sealed class RentedMemory<T> : IMemoryOwner<T>
{
    private readonly ArrayPool<T> _pool;
    private readonly bool _clearOnReturn;
    private T[]? _array;

    /// <summary>
    /// Rents an array of at least <paramref name="minimumLength"/> elements from
    /// <paramref name="pool"/>. Prefer the
    /// <see cref="ArrayPoolExtensions.RentMemory{T}(ArrayPool{T}, int, bool)"/> extension over
    /// calling this directly.
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
    internal RentedMemory(ArrayPool<T> pool, int minimumLength, bool clearOnReturn)
    {
        ArgumentNullException.ThrowIfNull(pool);
        ArgumentOutOfRangeException.ThrowIfNegative(minimumLength);

        _pool = pool;
        _array = pool.Rent(minimumLength);
        Length = minimumLength;
        _clearOnReturn = clearOnReturn;
    }

    /// <summary>
    /// The number of elements requested when renting. This is the length of the
    /// <see cref="Memory"/>/<see cref="Span"/> views, and is always less than or equal to
    /// <see cref="Capacity"/>. Remains readable after disposal.
    /// </summary>
    public int Length { get; }

    /// <summary>
    /// The actual length of the underlying rented array, which the pool may have sized larger than
    /// <see cref="Length"/>.
    /// </summary>
    /// <exception cref="ObjectDisposedException">The owner has been disposed.</exception>
    public int Capacity => GetArrayOrThrow().Length;

    /// <summary>
    /// The raw rented array, which may be larger than <see cref="Length"/>. Use this only to
    /// interoperate with APIs that require a <typeparamref name="T"/><c>[]</c> plus an explicit
    /// length/count; prefer <see cref="Memory"/> or <see cref="Span"/> otherwise. Do not retain a
    /// reference to it beyond this owner's scope — it returns to the pool on disposal.
    /// </summary>
    /// <exception cref="ObjectDisposedException">The owner has been disposed.</exception>
    public T[] Array => GetArrayOrThrow();

    /// <summary>
    /// A <see cref="Memory{T}"/> over the first <see cref="Length"/> elements of the rented array.
    /// </summary>
    /// <exception cref="ObjectDisposedException">The owner has been disposed.</exception>
    public Memory<T> Memory => new(GetArrayOrThrow(), 0, Length);

    /// <summary>
    /// A <see cref="Span{T}"/> over the first <see cref="Length"/> elements of the rented array.
    /// </summary>
    /// <exception cref="ObjectDisposedException">The owner has been disposed.</exception>
    public Span<T> Span => GetArrayOrThrow().AsSpan(0, Length);

    /// <summary>
    /// A reference to the element at <paramref name="index"/> within the rented array, assignable in
    /// place (for example <c>buffer[0] = value</c>).
    /// </summary>
    /// <param name="index">The zero-based element index.</param>
    /// <exception cref="ObjectDisposedException">The owner has been disposed.</exception>
    /// <exception cref="IndexOutOfRangeException"><paramref name="index"/> is outside the array bounds.</exception>
    public ref T this[int index] => ref GetArrayOrThrow()[index];

    /// <summary>
    /// The first <see cref="Length"/> elements of the rented array as an <see cref="ArraySegment{T}"/>,
    /// for APIs that take a <typeparamref name="T"/><c>[]</c> plus an offset and count.
    /// </summary>
    /// <exception cref="ObjectDisposedException">The owner has been disposed.</exception>
    public ArraySegment<T> AsArraySegment() => new(GetArrayOrThrow(), 0, Length);

    /// <summary>
    /// Creates a <see cref="Memory{T}"/> window over the rented array starting at
    /// <paramref name="start"/> for <paramref name="length"/> elements.
    /// </summary>
    /// <param name="start">The zero-based start index within the rented array.</param>
    /// <param name="length">The number of elements in the window.</param>
    /// <exception cref="ObjectDisposedException">The owner has been disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The requested window falls outside the bounds of the rented array.
    /// </exception>
    public Memory<T> AsMemory(int start, int length) => new(GetArrayOrThrow(), start, length);

    /// <summary>
    /// Creates a <see cref="Span{T}"/> window over the rented array starting at
    /// <paramref name="start"/> for <paramref name="length"/> elements.
    /// </summary>
    /// <param name="start">The zero-based start index within the rented array.</param>
    /// <param name="length">The number of elements in the window.</param>
    /// <exception cref="ObjectDisposedException">The owner has been disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The requested window falls outside the bounds of the rented array.
    /// </exception>
    public Span<T> AsSpan(int start, int length) => GetArrayOrThrow().AsSpan(start, length);

    /// <summary>
    /// Returns the rented array to the pool. Idempotent and thread-safe: concurrent or repeated
    /// calls (through any reference to this owner) return the array at most once.
    /// </summary>
    public void Dispose()
    {
        var array = Interlocked.Exchange(ref _array, null);
        if (array is null)
        {
            return;
        }

        _pool.Return(array, _clearOnReturn);
    }

    private T[] GetArrayOrThrow() =>
        _array ?? throw new ObjectDisposedException(nameof(RentedMemory<T>));
}
