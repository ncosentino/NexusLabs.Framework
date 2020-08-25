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
                () => new ContractException(conditionFailedMessage));

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

        public static void RequiresNotNull(
            object obj,
            string conditionFailedMessage) =>
            RequiresNotNull(
                obj,
                () => new ContractException(conditionFailedMessage));

        public static void RequiresNotNull(
            object obj,
            Func<Exception> exceptionCallback) =>
            Requires(
                obj != null,
                exceptionCallback);

        public static void RequiresNotNullOrEmpty<T>(
            IReadOnlyCollection<T> collection,
            string conditionFailedMessage) =>
            RequiresNotNullOrEmpty(
                collection,
                () => new ContractException(conditionFailedMessage));

        public static void RequiresNotNullOrEmpty<T>(
            IReadOnlyCollection<T> collection,
            Func<Exception> exceptionCallback) =>
            Requires(
                collection != null && collection.Count > 0,
                exceptionCallback);

        public static void RequiresNotNullOrEmpty(
            string str,
            string conditionFailedMessage) =>
            RequiresNotNullOrEmpty(
                str,
                () => new ContractException(conditionFailedMessage));

        public static void RequiresNotNullOrEmpty(
            string str,
            Func<Exception> exceptionCallback) =>
            Requires(
                !string.IsNullOrEmpty(str),
                exceptionCallback);

        public static void RequiresNotNullOrWhiteSpace(
            string str,
            string conditionFailedMessage) =>
            RequiresNotNullOrWhiteSpace(
                str,
                () => new ContractException(conditionFailedMessage));

        public static void RequiresNotNullOrWhiteSpace(
            string str,
            Func<Exception> exceptionCallback) =>
            Requires(
                !string.IsNullOrWhiteSpace(str),
                exceptionCallback);
    }
}
