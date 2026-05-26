using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace NexusLabs.Framework;

public readonly struct TriedNullEx<T> : IDisposable, IAsyncDisposable
{
    private static readonly Lazy<TriedNullEx<T?>> _default = new(() => new TriedNullEx<T?>(default(T?)));

    private readonly T? _value;
    private readonly Exception? _error;

    public TriedNullEx(T? value)
    {
        _value = value;
        _error = null;
    }

    public TriedNullEx([DisallowNull] Exception error)
    {
        ArgumentNullException.ThrowIfNull(error);

        _value = default;
        _error = error;
    }

    public static TriedNullEx<T?> Default => _default.Value;

    public T? Value
    {
        get
        {
            if (_error != null)
            {
                throw new InvalidOperationException(
                    $"Cannot access property '{nameof(Value)}' because the " +
                    $"'{nameof(Error)}' property has been set. See inner exception.",
                    _error);
            }

            return _value;
        }
    }

    [NotNull]
    public Exception Error
    {
        get
        {
            if (_error == null)
            {
                throw new InvalidOperationException(
                    $"Cannot access property '{nameof(Error)}' because the " +
                    $"'{nameof(Success)}' property has been set.");
            }

            return _error;
        }
    }

    public bool Success => _error == null;

    public static implicit operator TriedNullEx<T?>(T? value)
        => new(value);

    public static implicit operator TriedNullEx<T?>([DisallowNull] Exception error)
        => new(error);

    public static implicit operator T?([DisallowNull] TriedNullEx<T?> tried)
        => tried.Value;

    public static implicit operator Exception?([DisallowNull] TriedNullEx<T> tried)
        => tried.Error;

    public void Deconstruct(
        out bool Success,
        out T? Value)
    {
        Success = this.Success;
        Value = this.Success
            ? this.Value
            : default;
    }

    public void Deconstruct(
        out bool Success,
        out T? Value,
        out Exception? Error)
    {
        (Success, Value) = this;
        Error = this._error;
    }

    public TMatch Match<TMatch>(
        Func<T?, TMatch> successCallback,
        Func<Exception, TMatch> failCallback)
    {
        return Success
            ? successCallback(_value)
            : failCallback(_error!);
    }

    public Task<TMatch> MatchAsync<TMatch>(
        Func<T?, Task<TMatch>> successCallback,
        Func<Exception, Task<TMatch>> failCallback)
    {
        return Success
            ? successCallback(_value)
            : failCallback(_error!);
    }

    public Task MatchAsync(
        Func<T?, Task> successCallback,
        Func<Exception, Task> failCallback)
    {
        return Success
            ? successCallback(_value)
            : failCallback(_error!);
    }

    public override string ToString() => Success
        ? Value is null 
        ? string.Empty : Convert.ToString(Value) ?? string.Empty
        : $"{Error!.GetType()}: {Error.Message}\r\n" +
          $"{Error.StackTrace}";

    /// <summary>
    /// Disposes the wrapped value when <see cref="Success"/> is true and the value implements
    /// <see cref="IDisposable"/>. No-op when <see cref="Success"/> is false, the value is null,
    /// or the value is not disposable. The <see cref="Error"/> is never disposed.
    /// </summary>
    /// <remarks>
    /// For non-disposable <typeparamref name="T"/> the JIT specializes the type check to a
    /// compile-time constant and inlines this method to a no-op, so there is no runtime cost
    /// to declaring the type as <see cref="IDisposable"/>.
    /// </remarks>
    public void Dispose()
    {
        if (Success && _value is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    /// <summary>
    /// Asynchronously disposes the wrapped value when <see cref="Success"/> is true. Prefers
    /// <see cref="IAsyncDisposable"/>; falls back to synchronous <see cref="IDisposable"/>;
    /// otherwise no-op. The <see cref="Error"/> is never disposed.
    /// </summary>
    /// <remarks>
    /// For non-disposable <typeparamref name="T"/> the JIT specializes the type checks to
    /// compile-time constants and inlines this method to return a completed task, so there is
    /// no runtime cost to declaring the type as <see cref="IAsyncDisposable"/>.
    /// </remarks>
    public ValueTask DisposeAsync()
    {
        if (Success)
        {
            if (_value is IAsyncDisposable asyncDisposable)
            {
                return asyncDisposable.DisposeAsync();
            }

            if (_value is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        return ValueTask.CompletedTask;
    }
}
