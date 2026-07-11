using System;
using System.Buffers;

namespace NexusLabs.Framework.Buffers;

/// <summary>
/// Extension augmentations on <see cref="ArrayPool{T}"/>. Uses a C# <c>extension&lt;T&gt;(ArrayPool&lt;T&gt;)</c>
/// block so the helpers are callable in the same instance-method shape as the built-in BCL members
/// (<c>pool.Rent(n)</c>, <c>pool.RentSpan(n)</c>).
/// </summary>
public static class ArrayPoolExtensions
{
    extension<T>(ArrayPool<T> pool)
    {
        /// <summary>
        /// Rents an array of at least <paramref name="minimumLength"/> elements and returns a
        /// <see cref="RentedMemory{T}"/> owner whose disposal returns the array to this pool. Pair
        /// with <c>using</c> to bind the return to scope exit and drop the manual
        /// <c>try</c>/<c>finally</c>:
        /// <code>
        /// using var buffer = pool.RentMemory(capacity);
        /// var read = await stream.ReadAsync(buffer.Memory, cancellationToken).ConfigureAwait(false);
        /// Process(buffer.AsMemory(0, read));
        /// </code>
        /// The owner is a reference type that is safe to hold across <c>await</c>; copies share one
        /// owner, so the array is returned exactly once. It costs one small heap allocation for the
        /// owner (the array itself is pooled). Prefer <see cref="RentSpan(int, bool)"/> for
        /// synchronous code, which allocates nothing and cannot escape.
        /// </summary>
        /// <param name="minimumLength">
        /// The minimum number of elements required. The pool may return a larger array; read the
        /// granted size from <see cref="RentedMemory{T}.Capacity"/>.
        /// </param>
        /// <param name="clearOnReturn">
        /// Whether the array is cleared when returned to the pool on disposal. Set
        /// <see langword="true"/> when the buffer held sensitive data that must not linger in a
        /// pooled array handed to the next renter. Defaults to <see langword="false"/>.
        /// </param>
        /// <returns>A disposable owner whose disposal returns the array to this pool.</returns>
        /// <exception cref="ArgumentNullException">The pool is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="minimumLength"/> is negative.</exception>
        public RentedMemory<T> RentMemory(int minimumLength, bool clearOnReturn = false)
        {
            ArgumentNullException.ThrowIfNull(pool);

            return new RentedMemory<T>(pool, minimumLength, clearOnReturn);
        }

        /// <summary>
        /// Rents an array of at least <paramref name="minimumLength"/> elements and returns a
        /// <see cref="RentedSpan{T}"/> handle — the zero-allocation, <c>ref struct</c>,
        /// synchronous-only companion to <see cref="RentMemory(int, bool)"/> — whose disposal
        /// returns the array to this pool. Pair with <c>using</c> to bind the return to scope exit:
        /// <code>
        /// using var buffer = pool.RentSpan(capacity);
        /// var read = stream.Read(buffer.Span);
        /// Process(buffer.AsSpan(0, read));
        /// </code>
        /// The compiler guarantees a <see cref="RentedSpan{T}"/> can never escape to the heap (no
        /// boxing, fields, capture, collection storage, or crossing <c>await</c>), which rules out
        /// use-after-return bugs. Choose this for synchronous code; use
        /// <see cref="RentMemory(int, bool)"/> when you must hold the buffer across <c>await</c>.
        /// </summary>
        /// <param name="minimumLength">
        /// The minimum number of elements required. The pool may return a larger array; read the
        /// granted size from <see cref="RentedSpan{T}.Capacity"/>.
        /// </param>
        /// <param name="clearOnReturn">
        /// Whether the array is cleared when returned to the pool on disposal. Set
        /// <see langword="true"/> when the buffer held sensitive data that must not linger in a
        /// pooled array handed to the next renter. Defaults to <see langword="false"/>.
        /// </param>
        /// <returns>A disposable handle whose disposal returns the array to this pool.</returns>
        /// <exception cref="ArgumentNullException">The pool is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="minimumLength"/> is negative.</exception>
        public RentedSpan<T> RentSpan(int minimumLength, bool clearOnReturn = false)
        {
            ArgumentNullException.ThrowIfNull(pool);

            return new RentedSpan<T>(pool, minimumLength, clearOnReturn);
        }
    }
}
