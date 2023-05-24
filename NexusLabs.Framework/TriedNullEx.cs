using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

using NexusLabs.Contracts;

namespace NexusLabs.Framework
{
    public readonly struct TriedNullEx<T>
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
            ArgumentContract.RequiresNotNull(error, nameof(error));

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
            ? Convert.ToString(Value) ?? string.Empty
            : $"{Error!.GetType()}: {Error.Message}\r\n" +
              $"{Error.StackTrace}";
    }
}
