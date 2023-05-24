using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace NexusLabs.Contracts
{
    public static class Contract
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Requires(
            Func<bool> condition,
            string conditionFailedMessage) =>
            Requires(
                condition,
                () => conditionFailedMessage);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Requires(
            Func<bool> condition,
            Func<string> conditionFailedMessageCallback) =>
            Requires(
                condition,
                () => new ContractException(conditionFailedMessageCallback.Invoke()));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Requires(
            Func<bool> condition,
            Func<Exception> exceptionCallback)
        {
            Requires(
                condition.Invoke(),
                exceptionCallback);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Requires(
            bool condition,
            string conditionFailedMessage) =>
            Requires(
                condition,
                () => new ContractException(conditionFailedMessage));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Requires(
            bool condition,
            Func<Exception> exceptionCallback)
        {
            if (!condition)
            {
                throw exceptionCallback.Invoke();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RequiresNotNull(object obj) =>
            RequiresNotNull(
                obj,
                () => "The specified object was null.");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RequiresNotNull(
            object obj,
            string conditionFailedMessage) =>
            RequiresNotNull(
                obj,
                () => conditionFailedMessage);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RequiresNotNull(
            object obj,
            Func<string> conditionFailedMessageCallback) =>
            RequiresNotNull(
                obj,
                () => new ContractException(conditionFailedMessageCallback.Invoke()));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RequiresNotNull(
            object obj,
            Func<Exception> exceptionCallback) =>
            Requires(
                obj != null,
                exceptionCallback);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RequiresNotNullOrEmpty<T>(IReadOnlyCollection<T> collection) =>
             RequiresNotNullOrEmpty(
                 collection,
                 () => "The specified collection was null or empty.");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RequiresNotNullOrEmpty<T>(
            IReadOnlyCollection<T> collection,
            string conditionFailedMessage) =>
            RequiresNotNullOrEmpty(
                collection,
                () => conditionFailedMessage);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RequiresNotNullOrEmpty<T>(
            IReadOnlyCollection<T> collection,
            Func<string> conditionFailedMessageCallback) =>
            RequiresNotNullOrEmpty(
                collection,
                () => new ContractException(conditionFailedMessageCallback.Invoke()));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RequiresNotNullOrEmpty<T>(
            IReadOnlyCollection<T> collection,
            Func<Exception> exceptionCallback) =>
            Requires(
                collection != null && collection.Count > 0,
                exceptionCallback);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RequiresNotNullOrEmpty(string str) =>
            RequiresNotNullOrEmpty(
                str,
                () => "The specified string was null or empty.");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RequiresNotNullOrEmpty(
            string str,
            string conditionFailedMessage) =>
            RequiresNotNullOrEmpty(
                str,
                () => conditionFailedMessage);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RequiresNotNullOrEmpty(
            string str,
            Func<string> conditionFailedMessageCallback) =>
            RequiresNotNullOrEmpty(
                str,
                () => new ContractException(conditionFailedMessageCallback.Invoke()));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RequiresNotNullOrEmpty(
            string str,
            Func<Exception> exceptionCallback) =>
            Requires(
                !string.IsNullOrEmpty(str),
                exceptionCallback);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RequiresNotNullOrWhiteSpace(string str) =>
            RequiresNotNullOrWhiteSpace(
                str,
                () => "The specified string was null or whitespace.");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RequiresNotNullOrWhiteSpace(
            string str,
            string conditionFailedMessage) =>
            RequiresNotNullOrWhiteSpace(
                str,
                () => conditionFailedMessage);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RequiresNotNullOrWhiteSpace(
            string str,
            Func<string> conditionFailedMessageCallback) =>
            RequiresNotNullOrWhiteSpace(
                str,
                () => new ContractException(conditionFailedMessageCallback.Invoke()));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RequiresNotNullOrWhiteSpace(
            string str,
            Func<Exception> exceptionCallback) =>
            Requires(
                !string.IsNullOrWhiteSpace(str),
                exceptionCallback);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RequiresNotNullAndEmpty<T>(IReadOnlyCollection<T> collection) =>
            RequiresNotNullAndEmpty(
                collection,
                () => "The specified collection was null or not empty.");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RequiresNotNullAndEmpty<T>(
            IReadOnlyCollection<T> collection,
            string conditionFailedMessage) =>
            RequiresNotNullAndEmpty(
                collection,
                () => conditionFailedMessage);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RequiresNotNullAndEmpty<T>(
            IReadOnlyCollection<T> collection,
            Func<string> conditionFailedMessageCallback) =>
            RequiresNotNullAndEmpty(
                collection,
                () => new ContractException(conditionFailedMessageCallback.Invoke()));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RequiresNotNullAndEmpty<T>(
            IReadOnlyCollection<T> collection,
            Func<Exception> exceptionCallback) =>
            Requires(
                collection != null && collection.Count < 1,
                exceptionCallback);
    }
}
