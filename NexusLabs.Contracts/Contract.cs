using System;
using System.Collections.Generic;

namespace NexusLabs.Contracts
{
    public static class Contract
    {
        public static void Requires(
            Func<bool> condition,
            string conditionFailedMessage)
        {
            Requires(
                condition(),
                conditionFailedMessage);
        }

        public static void Requires(
            bool condition,
            string conditionFailedMessage)
        {
            if (!condition)
            {
                throw new ContractException(conditionFailedMessage);
            }
        }

        public static void RequiresNotNull(
            object obj,
            string conditionFailedMessage)
        {
            Requires(
                obj != null,
                conditionFailedMessage);
        }

        public static void RequiresNotNullOrEmpty<T>(
            IReadOnlyCollection<T> collection,
            string conditionFailedMessage)
        {
            Requires(
                collection != null && collection.Count > 0,
                conditionFailedMessage);
        }

        public static void RequiresNotNullOrEmpty(
            string str,
            string conditionFailedMessage)
        {
            Requires(
                !string.IsNullOrEmpty(str),
                conditionFailedMessage);
        }
    }
}
