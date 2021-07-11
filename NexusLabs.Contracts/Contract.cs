using System;
using System.Collections.Generic;

namespace NexusLabs.Contracts
{
    public static class Contract
    {
        public static void Requires(
            Func<bool> condition,
            string conditionFailedMessage) =>
            Requires(
                condition,
                () => conditionFailedMessage);

        public static void Requires(
            Func<bool> condition,
            Func<string> conditionFailedMessageCallback) =>
            Requires(
                condition,
                () => new ContractException(conditionFailedMessageCallback.Invoke()));

        public static void Requires(
            Func<bool> condition,
            Func<Exception> exceptionCallback)
        {
            Requires(
                condition.Invoke(),
                exceptionCallback);
        }

        public static void Requires(
            bool condition,
            string conditionFailedMessage) =>
            Requires(
                condition,
                () => new ContractException(conditionFailedMessage));

        public static void Requires(
            bool condition,
            Func<Exception> exceptionCallback)
        {
            if (!condition)
            {
                throw exceptionCallback.Invoke();
            }
        }

        public static void RequiresNotNull(object obj) =>
            RequiresNotNull(
                obj,
                () => "The specified object was null.");

        public static void RequiresNotNull(
            object obj,
            string conditionFailedMessage) =>
            RequiresNotNull(
                obj,
                () => conditionFailedMessage);

        public static void RequiresNotNull(
            object obj,
            Func<string> conditionFailedMessageCallback) =>
            RequiresNotNull(
                obj,
                () => new ContractException(conditionFailedMessageCallback.Invoke()));

        public static void RequiresNotNull(
            object obj,
            Func<Exception> exceptionCallback) =>
            Requires(
                obj != null,
                exceptionCallback);

        public static void RequiresNotNullOrEmpty<T>(IReadOnlyCollection<T> collection) =>
             RequiresNotNullOrEmpty(
                 collection,
                 () => "The specified collection was null or empty.");

        public static void RequiresNotNullOrEmpty<T>(
            IReadOnlyCollection<T> collection,
            string conditionFailedMessage) =>
            RequiresNotNullOrEmpty(
                collection,
                () => conditionFailedMessage);

        public static void RequiresNotNullOrEmpty<T>(
            IReadOnlyCollection<T> collection,
            Func<string> conditionFailedMessageCallback) =>
            RequiresNotNullOrEmpty(
                collection,
                () => new ContractException(conditionFailedMessageCallback.Invoke()));

        public static void RequiresNotNullOrEmpty<T>(
            IReadOnlyCollection<T> collection,
            Func<Exception> exceptionCallback) =>
            Requires(
                collection != null && collection.Count > 0,
                exceptionCallback);

        public static void RequiresNotNullOrEmpty(string str) =>
            RequiresNotNullOrEmpty(
                str,
                () => "The specified string was null or empty.");

        public static void RequiresNotNullOrEmpty(
            string str,
            string conditionFailedMessage) =>
            RequiresNotNullOrEmpty(
                str,
                () => conditionFailedMessage);

        public static void RequiresNotNullOrEmpty(
            string str,
            Func<string> conditionFailedMessageCallback) =>
            RequiresNotNullOrEmpty(
                str,
                () => new ContractException(conditionFailedMessageCallback.Invoke()));

        public static void RequiresNotNullOrEmpty(
            string str,
            Func<Exception> exceptionCallback) =>
            Requires(
                !string.IsNullOrEmpty(str),
                exceptionCallback);

        public static void RequiresNotNullOrWhiteSpace(string str) =>
            RequiresNotNullOrWhiteSpace(
                str,
                () => "The specified string was null or whitespace.");

        public static void RequiresNotNullOrWhiteSpace(
            string str,
            string conditionFailedMessage) =>
            RequiresNotNullOrWhiteSpace(
                str,
                () => conditionFailedMessage);

        public static void RequiresNotNullOrWhiteSpace(
            string str,
            Func<string> conditionFailedMessageCallback) =>
            RequiresNotNullOrWhiteSpace(
                str,
                () => new ContractException(conditionFailedMessageCallback.Invoke()));

        public static void RequiresNotNullOrWhiteSpace(
            string str,
            Func<Exception> exceptionCallback) =>
            Requires(
                !string.IsNullOrWhiteSpace(str),
                exceptionCallback);

        public static void RequiresNotNullAndEmpty<T>(IReadOnlyCollection<T> collection) =>
            RequiresNotNullAndEmpty(
                collection,
                () => "The specified collection was null or not empty.");

        public static void RequiresNotNullAndEmpty<T>(
            IReadOnlyCollection<T> collection,
            string conditionFailedMessage) =>
            RequiresNotNullAndEmpty(
                collection,
                () => conditionFailedMessage);

        public static void RequiresNotNullAndEmpty<T>(
            IReadOnlyCollection<T> collection,
            Func<string> conditionFailedMessageCallback) =>
            RequiresNotNullAndEmpty(
                collection,
                () => new ContractException(conditionFailedMessageCallback.Invoke()));

        public static void RequiresNotNullAndEmpty<T>(
            IReadOnlyCollection<T> collection,
            Func<Exception> exceptionCallback) =>
            Requires(
                collection != null && collection.Count < 1,
                exceptionCallback);
    }
}
