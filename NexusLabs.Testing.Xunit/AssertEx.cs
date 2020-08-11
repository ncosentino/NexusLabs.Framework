using System;
using System.Collections;
using System.Collections.Generic;

using Xunit;
using Xunit.Sdk;

namespace NexusLabs.Framework.Testing
{
    /// <summary>
    /// "Extensions" for the <see cref="Assert"/> class.
    /// </summary>
    public static class AssertEx
    {
        /// <summary>
        /// Asserts two values are equal.
        /// </summary>
        /// <typeparam name="T">The type of the item.</typeparam>
        /// <param name="expected">The expected value.</param>
        /// <param name="actual">The actual value.</param>
        /// <param name="message">The message to print.</param>
        public static void Equal<T>(
            T expected,
            T actual,
            string message)
            where T : IComparable
        {
            RunAssertion(
                message,
                () => Assert.Equal(expected, actual));
        }

        /// <summary>
        /// Asserts two values are equal.
        /// </summary>
        /// <param name="expected">The expected value.</param>
        /// <param name="actual">The actual value.</param>
        /// <param name="message">The message to print.</param>
        public static void Equal(
            object expected,
            object actual,
            string message)
        {
            RunAssertion(
                message,
                () => Assert.Equal(expected, actual));
        }

        /// <summary>
        /// Asserts two values are equal.
        /// </summary>
        /// <typeparam name="T">The type of the item.</typeparam>
        /// <param name="expected">The expected value.</param>
        /// <param name="actual">The actual value.</param>
        /// <param name="message">The message to print.</param>
        public static void Equal<T>(
            IEnumerable<T> expected,
            IEnumerable<T> actual,
            string message)
            where T : IComparable
        {
            RunAssertion(
                message,
                () => Assert.Equal(expected, actual));
        }

        /// <summary>
        /// Asserts two values are not equal.
        /// </summary>
        /// <typeparam name="T">The type of the item.</typeparam>
        /// <param name="expected">The expected value.</param>
        /// <param name="actual">The actual value.</param>
        /// <param name="message">The message to print.</param>
        public static void NotEqual<T>(
            T expected,
            T actual,
            string message)
            where T : IComparable
        {
            RunAssertion(
                message,
                () => Assert.NotEqual(expected, actual));
        }

        /// <summary>
        /// Asserts a value is in a range.
        /// </summary>
        /// <typeparam name="T">The type of the item.</typeparam>
        /// <param name="minimum">The minimum value.</param>
        /// <param name="maximum">The maximum value.</param>
        /// <param name="actual">The actual actual.</param>
        /// <param name="message">The message to print.</param>
        public static void InRange<T>(
            T minimum,
            T maximum,
            T actual,
            string message)
            where T : IComparable
        {
            RunAssertion(
                message,
                () => Assert.InRange(actual, minimum, maximum));
        }

        /// <summary>
        /// Asserts a collection is empty
        /// </summary>
        /// <typeparam name="T">the type of collection</typeparam>
        /// <param name="expected">the collection that should be empty</param>
        /// <param name="message">the message to print if it fails</param>
        public static void Empty<T>(T expected, string message) where T : IEnumerable
        {
            RunAssertion(message, () => Assert.Empty(expected));
        }

        private static void RunAssertion(string message, Action action)
        {
            try
            {
                action.Invoke();
            }
            catch (XunitException e)
            {
                throw new AssertExException(
                    $"{message}\r\n{e.Message}",
                    e);
            }
        }

        private sealed class AssertExException : XunitException
        {
            public AssertExException(string message, Exception inner)
                : base(message, inner)
            {
            }
        }
    }
}
