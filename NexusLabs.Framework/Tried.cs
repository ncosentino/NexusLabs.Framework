using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

using NexusLabs.Contracts;

namespace NexusLabs.Framework
{
    public readonly struct Tried<T>
    {
        private static readonly Lazy<Tried<T>> _failed = new(() => new());

        private readonly T? _value;

        public Tried([DisallowNull] T value)
        {
            ArgumentContract.RequiresNotNull(value, nameof(value));

            _value = value;
            Success = true;
        }

        public Tried()
        {
            _value = default;
            Success = false;
        }

        public static Tried<T> Failed => _failed.Value;

        [NotNull]
        public T Value
        {
            get
            {
                if (!Success)
                {
                    throw new InvalidOperationException(
                        $"Cannot access property '{nameof(Value)}' because the " +
                        $"'{nameof(Success)}' property is set to true.");
                }

                if (_value == null)
                {
                    throw new InvalidOperationException(
                        $"Invalid state. '{nameof(Value)}' is null but " +
                        $"'{nameof(Success)}' is set to true.");
                }

                return _value;
            }
        }

        public bool Success { get; init; }

        public static implicit operator Tried<T>([DisallowNull] T value)
            => new(value);

        public static implicit operator T([DisallowNull] Tried<T> tried)
            => tried.Value;

        public static implicit operator bool([DisallowNull] Tried<T> tried)
            => tried.Success;

        public void Deconstruct(
            out bool Success,
            out T? Value)
        {
            Success = this.Success;
            Value = this.Success
                ? this.Value
                : default;
        }

        public TMatch Match<TMatch>(
            Func<T, TMatch> successCallback,
            Func< TMatch> failCallback)
        {
            return Success
                ? successCallback(_value!)
                : failCallback();
        }

        public Task MatchAsync(
            Func<T, Task> successCallback,
            Func<Task> failCallback)
        {
            return Success
                ? successCallback(_value!)
                : failCallback();
        }

        public override string ToString() => Success
            ? Convert.ToString(Value) ?? string.Empty
            : "Failed";
    }
}
