using System;
using System.Diagnostics.CodeAnalysis;

using NexusLabs.Contracts;

namespace NexusLabs.Framework
{
    public sealed class TriedEx<T>
    {
        private readonly T? _value;

        public TriedEx([DisallowNull] T value)
        {
            ArgumentContract.RequiresNotNull(value, nameof(value));

            _value = value;
            Error = null;
        }

        public TriedEx([DisallowNull] Exception error)
        {
            ArgumentContract.RequiresNotNull(error, nameof(error));

            _value = default;
            Error = error;
        }

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

                if (_value == null)
                {
                    throw new InvalidOperationException(
                        $"Invalid state. '{nameof(Value)}' is null but " +
                        $"'{nameof(Error)}' is not set.");
                }

                return _value;
            }
        }

        public Exception? Error { get; }

        public bool Success => Error == null;

        public static implicit operator TriedEx<T>([DisallowNull] T value)
            => new(value);

        public static implicit operator TriedEx<T>([DisallowNull] Exception error)
            => new(error);

        public static implicit operator T([DisallowNull] TriedEx<T> tried)
            => tried.Value;

        public static implicit operator Exception?([DisallowNull] TriedEx<T> tried)
            => tried.Error;
    }
}
