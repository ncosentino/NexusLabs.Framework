using System;
using System.Collections.Generic;

namespace NexusLabs.Contracts
{
    public static class ArgumentContract
    {
        public static void Requires(
            Func<bool> condition,
            string parameterName,
            string conditionFailedMessage) =>
            Requires(
                condition,
                parameterName,
                () => conditionFailedMessage);

        public static void Requires(
            Func<bool> condition,
            string parameterName,
            Func<string> conditionFailedMessageCallback) =>
            Contract.Requires(
                condition,
                () => new ArgumentException(conditionFailedMessageCallback.Invoke(), parameterName));

        public static void Requires(
            bool condition,
            string parameterName,
            string conditionFailedMessage) =>
            Contract.Requires(
                condition,
                () => new ArgumentException(conditionFailedMessage, parameterName));

        public static void RequiresNotNull(
            object obj,
            string parameterName) =>
            RequiresNotNull(
                obj,
                parameterName,
                $"'{parameterName}' cannot be null.");

        public static void RequiresNotNull(
            object obj,
            string parameterName,
            string conditionFailedMessage) =>
            RequiresNotNull(
                obj,
                parameterName,
                () => conditionFailedMessage);

        public static void RequiresNotNull(
            object obj,
            string parameterName,
            Func<string> conditionFailedMessageCallback) =>
            Contract.RequiresNotNull(
                obj,
                () => new ArgumentNullException(parameterName, conditionFailedMessageCallback.Invoke()));

        public static void RequiresNotNullOrEmpty<T>(
            IReadOnlyCollection<T> collection,
            string parameterName) =>
            RequiresNotNullOrEmpty(
                collection,
                parameterName,
                $"'{parameterName}' cannot be null or empty.");

        public static void RequiresNotNullOrEmpty<T>(
            IReadOnlyCollection<T> collection,
            string parameterName,
            string conditionFailedMessage) =>
            Contract.RequiresNotNullOrEmpty(
                collection,
                () => new ArgumentException(conditionFailedMessage, parameterName));

        public static void RequiresNotNullOrEmpty(
            string str,
            string parameterName) =>
            RequiresNotNullOrEmpty(
                str,
                parameterName,
                $"'{parameterName}' cannot be null or empty.");

        public static void RequiresNotNullOrEmpty(
            string str,
            string parameterName,
            string conditionFailedMessage) =>
            Contract.RequiresNotNullOrEmpty(
                str,
                () => new ArgumentException(conditionFailedMessage, parameterName));

        public static void RequiresNotNullOrWhiteSpace(
            string str,
            string parameterName) =>
            RequiresNotNullOrWhiteSpace(
                str,
                parameterName,
                $"'{parameterName}' cannot be null or white space.");

        public static void RequiresNotNullOrWhiteSpace(
            string str,
            string parameterName,
            string conditionFailedMessage) =>
            Contract.RequiresNotNullOrWhiteSpace(
                str,
                () => new ArgumentException(conditionFailedMessage, parameterName));
    }
}
