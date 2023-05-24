using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace NexusLabs.Contracts
{
    public static class ArgumentContract
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Requires(
            Func<bool> condition,
            string parameterName,
            string conditionFailedMessage) =>
            Requires(
                condition,
                parameterName,
                () => conditionFailedMessage);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Requires(
            Func<bool> condition,
            string parameterName,
            Func<string> conditionFailedMessageCallback) =>
            Contract.Requires(
                condition,
                () => new ArgumentException(conditionFailedMessageCallback.Invoke(), parameterName));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Requires(
            bool condition,
            string parameterName,
            string conditionFailedMessage) =>
            Contract.Requires(
                condition,
                () => new ArgumentException(conditionFailedMessage, parameterName));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RequiresNotNull(
            object obj,
#if NET7_0_OR_GREATER
            [CallerArgumentExpression(nameof(obj))] string parameterName = null) =>
#else
            string parameterName) =>
#endif
            RequiresNotNull(
                obj,
                parameterName,
                $"'{parameterName}' cannot be null.");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RequiresNotNull(
            object obj,
            string parameterName,
            string conditionFailedMessage) =>
            RequiresNotNull(
                obj,
                parameterName,
                () => conditionFailedMessage);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RequiresNotNull(
            object obj,
            string parameterName,
            Func<string> conditionFailedMessageCallback) =>
            Contract.RequiresNotNull(
                obj,
                () => new ArgumentNullException(parameterName, conditionFailedMessageCallback.Invoke()));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RequiresNotNullOrEmpty<T>(
            IReadOnlyCollection<T> collection,
#if NET7_0_OR_GREATER
            [CallerArgumentExpression(nameof(collection))] string parameterName = null) =>
#else
            string parameterName) =>
#endif
            RequiresNotNullOrEmpty(
                collection,
                parameterName,
                $"'{parameterName}' cannot be null or empty.");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RequiresNotNullOrEmpty<T>(
            IReadOnlyCollection<T> collection,
            string parameterName,
            string conditionFailedMessage) =>
            Contract.RequiresNotNullOrEmpty(
                collection,
                () => new ArgumentException(conditionFailedMessage, parameterName));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RequiresNotNullOrEmpty(
            string str,
#if NET7_0_OR_GREATER
            [CallerArgumentExpression(nameof(str))] string parameterName = null) =>
#else
            string parameterName) =>
#endif
            RequiresNotNullOrEmpty(
                str,
                parameterName,
                $"'{parameterName}' cannot be null or empty.");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RequiresNotNullOrEmpty(
            string str,
            string parameterName,
            string conditionFailedMessage) =>
            Contract.RequiresNotNullOrEmpty(
                str,
                () => new ArgumentException(conditionFailedMessage, parameterName));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RequiresNotNullOrWhiteSpace(
            string str,
#if NET7_0_OR_GREATER
            [CallerArgumentExpression(nameof(str))] string parameterName = null) =>
#else
            string parameterName) =>
#endif
            RequiresNotNullOrWhiteSpace(
                str,
                parameterName,
                $"'{parameterName}' cannot be null or white space.");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RequiresNotNullOrWhiteSpace(
            string str,
            string parameterName,
            string conditionFailedMessage) =>
            Contract.RequiresNotNullOrWhiteSpace(
                str,
                () => new ArgumentException(conditionFailedMessage, parameterName));
    }
}
