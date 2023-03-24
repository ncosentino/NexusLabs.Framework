using System;

using Xunit;

namespace NexusLabs.Framework.Tests
{
    public sealed class TriedExTests
    {
        [Fact]
        private void Constructor_NullException_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new TriedEx<bool>(null));
        }

        [Fact]
        private void Constructor_NullValue_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new TriedEx<object>((object)null));
        }

        [Fact]
        private void ImplicitConversion_Boolean_ValuesAreEqual()
        {
            var input = true;
            var tried = new TriedEx<bool>(input);
            bool output = tried;

            Assert.Equal(input, output);
        }

        [Fact]
        private void ImplicitConversion_String_ValuesAreEqual()
        {
            var input = "this is a test";
            var tried = new TriedEx<string>(input);
            string output = tried;

            Assert.Equal(input, output);
        }

        [Fact]
        private void ImplicitConversion_Error_ExceptionIsSameObject()
        {
            var error = new InvalidOperationException("expected exception");
            var tried = new TriedEx<bool>(error);
            Exception output = tried;

            Assert.IsType<InvalidOperationException>(output);
            Assert.Same(error, output);
        }

        [Fact]
        private void ImplicitConversion_ValueWhenConstructedWithException_Throws()
        {
            var error = new InvalidOperationException("expected exception");
            var tried = new TriedEx<bool>(error);

            var exception = Assert.Throws<InvalidOperationException>(() => { bool _ = tried; });
            Assert.StartsWith("Cannot access ", exception.Message);
            Assert.Equal(error, exception.InnerException);
        }

        [Fact]
        private void Success_ConstructedWithException_IsFalse()
        {
            var error = new InvalidOperationException("expected exception");
            var tried = new TriedEx<bool>(error);
            Assert.False(
                tried.Success,
                $"{nameof(tried.Success)} was not expected value.");
            Assert.Equal(error, tried.Error);
        }

        [Fact]
        private void Success_ConstructedWithValue_IsTrue()
        {
            var tried = new TriedEx<bool>(false);
            Assert.True(
                tried.Success,
                $"{nameof(tried.Success)} was not expected value.");
            Assert.Null(tried.Error);
        }

        [Fact]
        private void Value_WhenConstructedWithException_Throws()
        {
            var error = new InvalidOperationException("expected exception");
            var tried = new TriedEx<bool>(error);

            var exception = Assert.Throws<InvalidOperationException>(() => { bool _ = tried.Value; });
            Assert.StartsWith("Cannot access ", exception.Message);
            Assert.Equal(error, exception.InnerException);
        }

        [Fact]
        private void ImplicitConversion_ValueToTriedReturnType_ExpectedValue()
        {
            var value = 123;
            TriedEx<int> TryDoSomething() => value;
            Assert.Equal<int>(value, TryDoSomething());
        }

        [Fact]
        private void ImplicitConversion_ExceptionToTriedReturnType_ExpectedError()
        {
            var error = new InvalidOperationException("expected exception");
            TriedEx<int> TryDoSomething() => error;
            Assert.Equal<Exception>(error, TryDoSomething());
        }

        [Fact]
        private void Deconstruct_FailedTriedIntType_SuccessAndValue()
        {
            var error = new InvalidOperationException("expected exception");
            TriedEx<int> TryDoSomething() => error;
            var (Success, Value) = TryDoSomething();
            Assert.False(
                Success,
                $"{nameof(Tried<int>.Success)} was not expected value.");
            Assert.Equal(default, Value);
        }

        [Fact]
        private void Deconstruct_FailedTriedIntType_SuccessValueError()
        {
            var error = new InvalidOperationException("expected exception");
            TriedEx<int> TryDoSomething() => error;
            var (Success, Value, Error) = TryDoSomething();
            Assert.False(
                Success,
                $"{nameof(Tried<int>.Success)} was not expected value.");
            Assert.Equal(default, Value);
            Assert.Equal(error, Error);
        }

        [Fact]
        private void Deconstruct_SuccessfulTriedIntType_SuccessAndValue()
        {
            var value = 123;
            TriedEx<int> TryDoSomething() => value;
            var (Success, Value) = TryDoSomething();
            Assert.True(
                Success,
                $"{nameof(Tried<int>.Success)} was not expected value.");
            Assert.Equal(value, Value);
        }

        [Fact]
        private void Deconstruct_SuccessfulTriedIntType_SuccessValueError()
        {
            var value = 123;
            TriedEx<int> TryDoSomething() => value;
            var (Success, Value, Error) = TryDoSomething();
            Assert.True(
                Success,
                $"{nameof(Tried<int>.Success)} was not expected value.");
            Assert.Equal(value, Value);
            Assert.Null(Error);
        }
    }
}
