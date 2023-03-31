using System;
using System.Diagnostics.CodeAnalysis;

using NexusLabs.Contracts;

namespace NexusLabs.Framework
{
    public sealed class TriedNullEx<T>
    {
        private static readonly Lazy<TriedNullEx<T?>> _default = new(() => new TriedNullEx<T?>(default(T?)));

        private readonly T? _value;

        public TriedNullEx(T? value)
        {
            _value = value;
            Error = null;
        }

        public TriedNullEx([DisallowNull] Exception error)
        {
            ArgumentContract.RequiresNotNull(error, nameof(error));

            _value = default;
            Error = error;
        }

        public static TriedNullEx<T?> Default => _default.Value;

        [NotNull]
        public T Value
        {
            get
            {
                if (Error != null)
                {
                    throw new InvalidOperationException(
                        $"Cannot access property '{nameof(Value)}' because the " +
                        $"'{nameof(Error)}' property has been set. See inner exception.",
                        Error);
                }

                return _value;
            }
        }

        public Exception? Error { get; }

        public bool Success => Error == null;

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
            Error = this.Error;
        }

        public override string ToString() => Success
            ? Convert.ToString(Value) ?? string.Empty
            : $"{Error.GetType()}: {Error.Message}\r\n" +
              $"{Error.StackTrace}";
    }
}
