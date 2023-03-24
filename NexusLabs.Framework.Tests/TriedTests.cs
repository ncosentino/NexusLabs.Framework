using System;

using Xunit;

namespace NexusLabs.Framework.Tests
{
    public sealed class TriedTests
    {
        [Fact]
        private void Constructor_NullValue_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new Tried<object>(null));
        }

        // ambiguous when T is type bool
        //[Fact]
        //private void ImplicitConversion_Boolean_ValuesAreEqual()
        //{
        //    var input = true;
        //    var tried = new Tried<bool>(input);
        //    bool output = tried;

        //    Assert.Equal(input, output);
        //}

        [Fact]
        private void ImplicitConversion_String_ValuesAreEqual()
        {
            var input = "this is a test";
            var tried = new Tried<string>(input);
            string output = tried;

            Assert.Equal(input, output);
        }

        [Fact]
        private void Success_ConstructedWithFail_IsFalse()
        {
            var tried = Tried<bool>.Failed;
            Assert.False(
                tried.Success,
                $"{nameof(tried.Success)} was not expected value.");
        }

        [Fact]
        private void Success_ConstructedWithValue_IsTrue()
        {
            var value = false;
            var tried = new Tried<bool>(value);
            Assert.True(
                tried.Success,
                $"{nameof(tried.Success)} was not expected value.");
            Assert.Equal(value, tried.Value);
        }

        [Fact]
        private void Value_WhenConstructedWithException_Throws()
        {
            var tried = Tried<bool>.Failed;

            var exception = Assert.Throws<InvalidOperationException>(() => { bool _ = tried.Value; });
            Assert.StartsWith("Cannot access ", exception.Message);
            Assert.Null(exception.InnerException);
        }

        [Fact]
        private void ImplicitConversion_ValueToTriedReturnTypeInt_ExpectedValue()
        {
            var value = 123;
            Tried<int> TryDoSomething() => value;
            Assert.Equal<int>(value, TryDoSomething());
        }

        [Fact]
        private void ImplicitConversion_SuccessfulTried_ExpectedValue()
        {
            var value = 123;
            Tried<int> TryDoSomething() => value;
            Assert.True(
                TryDoSomething(),
                $"{nameof(Tried<int>.Success)} was not expected value.");
        }

        [Fact]
        private void ImplicitConversion_FailedTried_ExpectedValue()
        {
            Tried<int> TryDoSomething() => Tried<int>.Failed;
            Assert.False(
                TryDoSomething(),
                $"{nameof(Tried<int>.Success)} was not expected value.");
        }

        [Fact]
        private void Deconstruct_FailedTriedIntType_SuccessAndValue()
        {
            Tried<int> TryDoSomething() => Tried<int>.Failed;
            var (Success, Value) = TryDoSomething();
            Assert.False(
                Success,
                $"{nameof(Tried<int>.Success)} was not expected value.");
            Assert.Equal(default, Value);
        }

        [Fact]
        private void Deconstruct_SuccessfulTriedIntType_SuccessAndValue()
        {
            var value = 123;
            Tried<int> TryDoSomething() => value;
            var (Success, Value) = TryDoSomething();
            Assert.True(
                Success,
                $"{nameof(Tried<int>.Success)} was not expected value.");
            Assert.Equal(value, Value);
        }

        [Fact]
        private void ToString_Failed_Failed()
        {
            Tried<int> TryDoSomething() => Tried<int>.Failed;

            var tostring = TryDoSomething().ToString();
            Assert.Equal("Failed", tostring);
        }

        [Fact]
        private void ToString_SuccessIntType_ContainsIntValue()
        {
            var value = 123;
            Tried<int> TryDoSomething() => value;

            var tostring = TryDoSomething().ToString();
            Assert.Equal(value.ToString(), tostring);
        }
    }
}
