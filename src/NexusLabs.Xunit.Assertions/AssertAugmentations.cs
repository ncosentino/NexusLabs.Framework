using System;
using System.Collections;
using System.Net;
using System.Net.Http;
using System.Text.Json;

using NexusLabs.Framework;

using Xunit;
using Xunit.Sdk;

namespace NexusLabs.Xunit.Assertions;

/// <summary>
/// xUnit.v3 <see cref="Assert"/> augmentations that integrate with NexusLabs.Framework
/// result-pattern types (<see cref="TriedEx{T}"/>, <see cref="TriedNullEx{T}"/>) and
/// HTTP response shapes. Uses C# 14 <c>extension(Assert)</c> blocks so the helpers are
/// callable as <c>Assert.TrySucceeded(...)</c>, <c>Assert.HttpRequestHasResponse&lt;T&gt;(...)</c>,
/// etc. — same call site shape as the built-in xUnit assertions.
/// </summary>
/// <remarks>
/// Reference implementation derived from internal NexusLabs test infrastructure;
/// extracted into a reusable package as a successor to the deprecated
/// <c>NexusLabs.Testing.Xunit</c> 0.x line.
/// </remarks>
public static class AssertAugmentations
{
    extension(Assert)
    {
        /// <summary>
        /// Same as <see cref="Assert.True(bool, string)"/> but the message is lazily
        /// produced via <paramref name="message"/> only on failure, avoiding string
        /// allocation for the happy path.
        /// </summary>
        public static void True(
            bool value,
            Func<string> message)
        {
            if (value)
            {
                return;
            }

            Assert.True(
                value,
                message.Invoke());
        }

        /// <summary>
        /// Same as <see cref="Assert.False(bool, string)"/> but the message is lazily
        /// produced via <paramref name="message"/> only on failure.
        /// </summary>
        public static void False(
            bool value,
            Func<string> message)
        {
            if (!value)
            {
                return;
            }

            Assert.False(
                value,
                message.Invoke());
        }

        /// <summary>
        /// Asserts two strings are equal after normalizing CRLF to LF on both sides.
        /// Useful when comparing strings that originate from different platforms or
        /// from sources that emit mixed line endings.
        /// </summary>
        public static void EqualIgnoreLineEndingStyle(
            string expected,
            string actual,
            string message = "The values were not equal.") =>
            Equal(
                expected.Replace("\r\n", "\n"),
                actual.Replace("\r\n", "\n"),
                message);

        /// <summary>
        /// Asserts the collection is not empty, attaching <paramref name="message"/>
        /// to the failure exception for context.
        /// </summary>
        public static void NotEmpty(
            IEnumerable collection,
            string message) =>
            NotEmpty(
                collection,
                () => message);

        /// <summary>
        /// Asserts the collection is not empty. <paramref name="getMessageCallback"/>
        /// is invoked only on failure.
        /// </summary>
        public static void NotEmpty(
            IEnumerable collection,
            Func<string> getMessageCallback)
        {
            try
            {
                Assert.NotEmpty(collection);
            }
            catch (NotEmptyException ex)
            {
                throw new XunitExceptionWithMessage<NotEmptyException>(
                    getMessageCallback.Invoke(),
                    ex);
            }
        }

        /// <summary>
        /// Asserts a successful HTTP response wrapped in <see cref="TriedEx{T}"/>
        /// (string body), then deserializes the body JSON into <typeparamref name="T"/>
        /// and asserts non-null.
        /// </summary>
        public static T HttpRequestHasResponse<T>(
            TriedEx<string> actual,
            string message = "Unexpected result from HTTP request.")
            where T : class
        {
            var responseContent = TrySucceeded(
                actual,
                $"The result was not successful.\r\n{message}");
            T? response = JsonSerializer.Deserialize<T>(responseContent);
            NotNull(
                response,
                $"The parsed JSON result was null.\r\n{message}");
            return response!;
        }

        /// <summary>
        /// Asserts an HTTP request failed (the <see cref="TriedEx{T}"/> is unsuccessful)
        /// with a <see cref="HttpRequestException"/> matching the expected status code.
        /// </summary>
        public static HttpRequestException HttpRequestFailed<T>(
            TriedEx<T> actual,
            HttpStatusCode expectedStatusCode,
            string message = "Unexpected result from HTTP request.")
            => HttpRequestFailed<T, HttpRequestException>(
                actual,
                expectedStatusCode,
                message);

        /// <summary>
        /// Asserts an HTTP request failed with a specific exception type
        /// (<typeparamref name="TException"/>) and a specific status code.
        /// </summary>
        public static TException HttpRequestFailed<T, TException>(
            TriedEx<T> actual,
            HttpStatusCode expectedStatusCode,
            string message)
            where TException : HttpRequestException
        {
            var exception = TryFailed<T, TException>(actual, message);
            Assert.True(
                exception.StatusCode.HasValue,
                $"{message}\r\n" +
                $"There was no status code on the exception:\r\n" +
                $"{ExceptionHelper.BuildExceptionMessage(exception)}");
            Equal(expectedStatusCode, exception.StatusCode.Value, message);
            return exception;
        }

        /// <summary>
        /// Asserts the <see cref="TriedEx{T}"/> failed with a specific exception type
        /// (<typeparamref name="TException"/>) and returns the captured exception.
        /// </summary>
        public static TException TryFailed<T, TException>(
            TriedEx<T> actual,
            string message)
            where TException : Exception
        {
            TException? actualException = null;
            actual.Match<T>(
                _ => throw new XunitException(
                    $"{message}\r\n" +
                    $"The {nameof(TriedEx<T>)} instance was successful and was expected to fail with {typeof(TException)}."),
                error =>
                {
                    if (!error.GetType().IsAssignableTo(typeof(TException)))
                    {
                        throw new XunitException(
                            $"{message}\r\n" +
                            $"The error type '{error.GetType()}' was not of " +
                            $"type {typeof(TException)}. See inner exception for details.",
                            error);
                    }

                    actualException = (TException)error;
                    return default!;
                });
            return actualException!;
        }

        /// <summary>
        /// Same as <see cref="TryFailed{T, TException}(TriedEx{T}, string)"/> but for the
        /// nullable-aware <see cref="TriedNullEx{T}"/>.
        /// </summary>
        public static TException TryFailed<T, TException>(
            TriedNullEx<T?> actual,
            string message)
            where TException : Exception
        {
            TException? actualException = null;
            actual.Match<T?>(
                _ => throw new XunitException(
                    $"{message}\r\n" +
                    $"The {nameof(TriedNullEx<T?>)} instance was successful and was expected to fail with {typeof(TException)}."),
                error =>
                {
                    if (!error.GetType().IsAssignableTo(typeof(TException)))
                    {
                        throw new XunitException(
                            $"{message}\r\n" +
                            $"The error type '{error.GetType()}' was not of " +
                            $"type {typeof(TException)}. See inner exception for details.",
                            error);
                    }

                    actualException = (TException)error;
                    return default!;
                });
            return actualException!;
        }

        /// <summary>
        /// Asserts the <see cref="TriedEx{T}"/> is successful and returns its value.
        /// On failure the assertion message includes the captured exception chain via
        /// <see cref="ExceptionHelper.BuildExceptionMessage(Exception?)"/>.
        /// </summary>
        public static T TrySucceeded<T>(
            TriedEx<T> actual,
            string message)
        {
            True(
                actual.Success,
                () =>
                    $"{message}\r\n" +
                    $"The error was not null:\r\n" +
                    $"{ExceptionHelper.BuildExceptionMessage(actual)}");
            return actual;
        }

        /// <summary>
        /// Same as <see cref="TrySucceeded{T}(TriedEx{T}, string)"/> but for the
        /// nullable-aware <see cref="TriedNullEx{T}"/>.
        /// </summary>
        public static T? TrySucceeded<T>(
            TriedNullEx<T?> actual,
            string message)
        {
            True(
                actual.Success,
                () =>
                    $"{message}\r\n" +
                    $"The error was not null:\r\n" +
                    $"{ExceptionHelper.BuildExceptionMessage(actual)}");
            return actual;
        }

        /// <summary>
        /// Asserts the value is not null, attaching <paramref name="message"/> on failure.
        /// </summary>
        public static void NotNull<T>(
            T? actual,
            string message)
            where T : class =>
            NotNull<T>(actual, () => message);

        /// <summary>
        /// Asserts the value is not null. <paramref name="getMessageCallback"/> is
        /// invoked only on failure.
        /// </summary>
        public static void NotNull<T>(T? actual, Func<string> getMessageCallback)
            where T : class
        {
            try
            {
                Assert.NotNull(actual);
            }
            catch (NotNullException ex)
            {
                throw new XunitExceptionWithMessage<NotNullException>(getMessageCallback.Invoke(), ex);
            }
        }

        /// <summary>
        /// Asserts the value is null, attaching <paramref name="message"/> on failure.
        /// </summary>
        public static void Null<T>(
            T? actual,
            string message)
            where T : class
            => Null<T>(actual, () => message);

        /// <summary>
        /// Asserts the value is null. <paramref name="getMessageCallback"/> is
        /// invoked only on failure.
        /// </summary>
        public static void Null<T>(T? actual, Func<string> getMessageCallback)
            where T : class
        {
            try
            {
                Assert.Null(actual);
            }
            catch (NullException ex)
            {
                throw new XunitExceptionWithMessage<NullException>(getMessageCallback.Invoke(), ex);
            }
        }

        /// <summary>
        /// Asserts equality between two comparable values, attaching
        /// <paramref name="message"/> on failure.
        /// </summary>
        public static void Equal<T>(T expected, T actual, string message)
           where T : IComparable
        {
            try
            {
                Assert.Equal(expected, actual);
            }
            catch (EqualException ex)
            {
                throw new XunitExceptionWithMessage<EqualException>(message, ex);
            }
        }

        /// <summary>
        /// Asserts the value falls within [min, max], attaching <paramref name="message"/>
        /// on failure.
        /// </summary>
        public static void InRange<T>(T actual, T min, T max, string message)
            where T : IComparable
        {
            try
            {
                Assert.InRange(actual, min, max);
            }
            catch (InRangeException ex)
            {
                throw new XunitExceptionWithMessage<InRangeException>(message, ex);
            }
        }

        /// <summary>
        /// Shorthand for <c>GreaterThan(0, actual, message)</c>.
        /// </summary>
        public static void GreaterThanZero(int actual, string message)
        {
            GreaterThan(0, actual, message);
        }

        /// <summary>
        /// Asserts <paramref name="actual"/> is strictly greater than
        /// <paramref name="expectedMinimum"/>.
        /// </summary>
        public static void GreaterThan<T>(T expectedMinimum, T actual, string message)
            where T : IComparable
        {
            try
            {
                Assert.True(actual.CompareTo(expectedMinimum) > 0, "Value must be greater than minimum");
            }
            catch (TrueException)
            {
                throw new XunitExceptionWithMessage<InRangeException>(
                    message,
                    InRangeException.ForValueNotInRange(actual, expectedMinimum, int.MaxValue));
            }
        }

        /// <summary>
        /// Asserts <paramref name="actual"/> is greater than or equal to
        /// <paramref name="expectedMinimum"/>.
        /// </summary>
        public static void GreaterThanOrEqual<T>(T expectedMinimum, T actual, string message)
            where T : IComparable
        {
            try
            {
                Assert.True(actual.CompareTo(expectedMinimum) >= 0, "Value must be greater than or equal to minimum");
            }
            catch (TrueException)
            {
                throw new XunitExceptionWithMessage<InRangeException>(
                    message,
                    InRangeException.ForValueNotInRange(actual, expectedMinimum, int.MaxValue));
            }
        }

        /// <summary>
        /// Asserts <paramref name="actual"/> is strictly less than
        /// <paramref name="expectedMaximum"/>.
        /// </summary>
        public static void LessThan<T>(T expectedMaximum, T actual, string message)
            where T : IComparable
        {
            try
            {
                Assert.True(actual.CompareTo(expectedMaximum) < 0, "Value must be less than maximum");
            }
            catch (TrueException)
            {
                throw new XunitExceptionWithMessage<InRangeException>(
                    message,
                    InRangeException.ForValueNotInRange(actual, int.MinValue, expectedMaximum));
            }
        }

        /// <summary>
        /// Asserts <paramref name="actual"/> is less than or equal to
        /// <paramref name="expectedMaximum"/>.
        /// </summary>
        public static void LessThanOrEqual<T>(T expectedMaximum, T actual, string message)
            where T : IComparable
        {
            try
            {
                Assert.True(actual.CompareTo(expectedMaximum) <= 0, "Value must be less than or equal to maximum");
            }
            catch (TrueException)
            {
                throw new XunitExceptionWithMessage<InRangeException>(
                    message,
                    InRangeException.ForValueNotInRange(actual, int.MinValue, expectedMaximum));
            }
        }

        /// <summary>
        /// Asserts the collection is empty, attaching <paramref name="message"/>
        /// on failure.
        /// </summary>
        public static void Empty(IEnumerable collection, string message)
        {
            try
            {
                Assert.Empty(collection);
            }
            catch (EmptyException ex)
            {
                throw new XunitExceptionWithMessage<EmptyException>(message, ex);
            }
        }

        /// <summary>
        /// Asserts the HTTP response was successful (2xx). On failure, the message
        /// includes the actual status code and response body for debugging.
        /// </summary>
        public static HttpStatusCode HttpSuccess(HttpResponseMessage actual)
        {
            if (actual.IsSuccessStatusCode)
            {
                return actual.StatusCode;
            }

            Assert.Fail(
                $"Response failed with status code: {actual.StatusCode}\r\n" +
                $"Body: {actual.Content.ReadAsStringAsync().GetAwaiter().GetResult()}");
            return actual.StatusCode;
        }

        /// <summary>
        /// Asserts the HTTP response status matches <paramref name="expectedStatusCode"/>
        /// exactly. On mismatch, the message includes the actual status and body.
        /// </summary>
        public static HttpStatusCode HttpFailed(
            HttpStatusCode expectedStatusCode,
            HttpResponseMessage actual)
        {
            if (actual.StatusCode == expectedStatusCode)
            {
                return actual.StatusCode;
            }

            Assert.Fail(
                $"Response failed with status code: {actual.StatusCode}\r\n" +
                $"Body: {actual.Content.ReadAsStringAsync().GetAwaiter().GetResult()}");
            return actual.StatusCode;
        }

        /// <summary>
        /// Asserts the HTTP response status is NOT a 2xx success code. On a successful
        /// response, the failure message includes the body for debugging.
        /// </summary>
        public static HttpStatusCode HttpNotOk(
            HttpResponseMessage actual)
        {
            if ((int)actual.StatusCode < 200 ||
                (int)actual.StatusCode > 299)
            {
                return actual.StatusCode;
            }

            Assert.Fail(
                $"Response had status code: {actual.StatusCode}\r\n" +
                $"Body: {actual.Content.ReadAsStringAsync().GetAwaiter().GetResult()}");
            return actual.StatusCode;
        }
    }

    /// <summary>
    /// Wraps an xUnit exception type with an additional context message prepended.
    /// Used by the augmentation methods that take a user-supplied <c>message</c>
    /// argument so failures show both the framework-generated reason AND the
    /// caller's contextual hint.
    /// </summary>
    private sealed class XunitExceptionWithMessage<TException> : XunitException
        where TException : XunitException
    {
        public XunitExceptionWithMessage(
            string message,
            TException wrapped) :
            base($"{message}\r\n{wrapped.Message}")
        {
        }
    }
}
