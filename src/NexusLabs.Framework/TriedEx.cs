using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace NexusLabs.Framework;

// NLF0002 / NLF0003 protect consumers from accessing Value/Error without first
// checking Success. This file IS the implementation of TriedEx<T> — the getters
// must access the underlying fields directly, implicit-conversion operators
// must forward without an explicit guard the analyzer can see, and several
// members use the ternary `Success ? this.Value : default` pattern that the
// analyzer cannot flow-analyse through. The consumer-facing protection still
// applies everywhere else.
#pragma warning disable NLF0002 // Accessing 'Value' without guarding Success
#pragma warning disable NLF0003 // Accessing 'Error' without guarding !Success

public readonly struct TriedEx<T> : IDisposable, IAsyncDisposable
{
    private readonly T? _value;
    private readonly Exception? _error;

    public TriedEx([DisallowNull] T value)
    {
        ArgumentNullException.ThrowIfNull(value);

        _value = value;
        _error = null;
    }

    public TriedEx([DisallowNull] Exception error)
    {
        ArgumentNullException.ThrowIfNull(error);

        _value = default;
        _error = error;
    }

    [NotNull]
    public T Value
    {
        get
        {
            if (_error != null)
            {
                throw new InvalidOperationException(
                    $"Cannot access property '{nameof(Value)}' because the " +
                    $"'{nameof(Error)}' property has been set. See inner exception.",
                    Error);
            }

            return _value!;
        }
    }

    [NotNull]
    public Exception Error
    {
        get
        {
            if (Success)
            {
                throw new InvalidOperationException(
                    $"Cannot access property '{nameof(Error)}' because the " +
                    $"'{nameof(Success)}' property has been set.");
            }

            return _error!;
        }
    }

    public bool Success => _error == null;

    public static implicit operator TriedEx<T>([DisallowNull] T value)
        => new(value);

    public static implicit operator TriedEx<T>([DisallowNull] Exception error)
        => new(error);

    public static implicit operator T([DisallowNull] TriedEx<T> tried)
        => tried.Value;

    public static implicit operator Exception?([DisallowNull] TriedEx<T> tried)
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
        Func<T, TMatch> successCallback,
        Func<Exception, TMatch> failCallback)
    {
        return Success
            ? successCallback(_value!)
            : failCallback(_error!);
    }

    public void Match(
        Action<T> successCallback,
        Action<Exception> failCallback)
    {
        if (Success)
        {
            successCallback(_value!);
        }
        else
        {
            failCallback(_error!);
        }
    }

    public Task<TMatch> MatchAsync<TMatch>(
        Func<T, Task<TMatch>> successCallback,
        Func<Exception, Task<TMatch>> failCallback)
    {
        return Success
            ? successCallback(_value!)
            : failCallback(_error!);
    }

    public Task MatchAsync(
        Func<T, Task> successCallback,
        Func<Exception, Task> failCallback)
    {
        return Success
            ? successCallback(_value!)
            : failCallback(_error!);
    }

    public override string ToString() => Success
        ? Convert.ToString(Value) ?? string.Empty
        : $"{Error!.GetType()}: {Error.Message}\r\n" +
          $"{Error.StackTrace}";

    /// <summary>
    /// Disposes the wrapped value when <see cref="Success"/> is true and the value implements
    /// <see cref="IDisposable"/>. No-op when <see cref="Success"/> is false or the value is not
    /// disposable. The <see cref="Error"/> is never disposed.
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
