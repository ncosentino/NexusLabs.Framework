using System;

using Xunit;

namespace NexusLabs.Framework.Tests
{
    public sealed class TriedNullExTests
    {
        [Fact]
        private void Constructor_NullException_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new TriedNullEx<bool>(null));
        }

        [Fact]
        private void Constructor_NullValue_Allowed()
        {
            var tried = new TriedNullEx<object>((object)null);
            Assert.Null(tried.Value);
        }

        [Fact]
        private void ImplicitConversion_Boolean_ValuesAreEqual()
        {
            var input = true;
            var tried = new TriedNullEx<bool>(input);
            bool output = tried;

            Assert.Equal(input, output);
        }

        [Fact]
        private void ImplicitConversion_String_ValuesAreEqual()
        {
            var input = "this is a test";
            var tried = new TriedNullEx<string>(input);
            string output = tried;

            Assert.Equal(input, output);
        }

        [Fact]
        private void ImplicitConversion_Error_ExceptionIsSameObject()
        {
            var error = new InvalidOperationException("expected exception");
            var tried = new TriedNullEx<bool>(error);
            Exception output = tried;

            Assert.IsType<InvalidOperationException>(output);
            Assert.Same(error, output);
        }

        [Fact]
        private void ImplicitConversion_ValueWhenConstructedWithException_Throws()
        {
            var error = new InvalidOperationException("expected exception");
            var tried = new TriedNullEx<bool>(error);

            var exception = Assert.Throws<InvalidOperationException>(() => { bool _ = tried; });
            Assert.StartsWith("Cannot access ", exception.Message);
            Assert.Equal(error, exception.InnerException);
        }

        [Fact]
        private void Success_ConstructedWithException_IsFalse()
        {
            var error = new InvalidOperationException("expected exception");
            var tried = new TriedNullEx<bool>(error);
            Assert.False(
                tried.Success,
                $"{nameof(tried.Success)} was not expected value.");
            Assert.Equal(error, tried.Error);
        }

        [Fact]
        private void Success_ConstructedWithValue_IsTrue()
        {
            var tried = new TriedNullEx<bool>(false);
            Assert.True(
                tried.Success,
                $"{nameof(tried.Success)} was not expected value.");
            Assert.Null(tried.Error);
        }

        [Fact]
        private void Value_WhenConstructedWithException_Throws()
        {
            var error = new InvalidOperationException("expected exception");
            var tried = new TriedNullEx<bool>(error);

            var exception = Assert.Throws<InvalidOperationException>(() => { bool _ = tried.Value; });
            Assert.StartsWith("Cannot access ", exception.Message);
            Assert.Equal(error, exception.InnerException);
        }

        [Fact]
        private void ImplicitConversion_ValueToTriedReturnType_ExpectedValue()
        {
            var value = 123;
            TriedNullEx<int> TryDoSomething() => value;
            Assert.Equal<int>(value, TryDoSomething());
        }

        [Fact]
        private void ImplicitConversion_NullValueToTriedReturnType_ExpectedValue()
        {
            int? value = null;
            TriedNullEx<int> TryDoSomething() => value;
            Assert.Null(TryDoSomething());
        }

        [Fact]
        private void ImplicitConversion_ExceptionToTriedReturnType_ExpectedError()
        {
            var error = new InvalidOperationException("expected exception");
            TriedNullEx<int> TryDoSomething() => error;
            Assert.Equal<Exception>(error, TryDoSomething());
        }

        [Fact]
        private void Deconstruct_FailedTriedIntType_SuccessAndValue()
        {
            var error = new InvalidOperationException("expected exception");
            TriedNullEx<int> TryDoSomething() => error;
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
            TriedNullEx<int> TryDoSomething() => error;
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
            TriedNullEx<int> TryDoSomething() => value;
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
            TriedNullEx<int> TryDoSomething() => value;
            var (Success, Value, Error) = TryDoSomething();
            Assert.True(
                Success,
                $"{nameof(Tried<int>.Success)} was not expected value.");
            Assert.Equal(value, Value);
            Assert.Null(Error);
        }

        [Fact]
        private void ToString_Failed_ContainsExceptionInformation()
        {
            var error = new InvalidOperationException("expected");
            TriedNullEx<int> TryDoSomething()
            {
                try
                {
                    throw error;
                }
                catch (Exception ex)
                {
                    return ex;
                }

                throw new InvalidOperationException("not expected");
            };

            var tostring = TryDoSomething().ToString();
            Assert.StartsWith(error.GetType().ToString(), tostring);
            Assert.Contains(error.Message, tostring);
        }

        [Fact]
        private void ToString_SuccessIntType_ContainsIntValue()
        {
            var value = 123;
            TriedNullEx<int> TryDoSomething() => value;

            var tostring = TryDoSomething().ToString();
            Assert.Equal(value.ToString(), tostring);
        }

        [Fact]
        private void Default_Int_0Value()
        {
            var tried = TriedNullEx<int>.Default;
            Assert.Equal(0, tried.Value);
            Assert.Null(tried.Error);
        }

        [Fact]
        private void Default_IntNullable_NullValue()
        {
            var tried = TriedNullEx<int?>.Default;
            Assert.Null(tried.Value);
            Assert.Null(tried.Error);
        }
    }
}
