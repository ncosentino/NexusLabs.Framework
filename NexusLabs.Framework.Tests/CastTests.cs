using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using Xunit;

namespace NexusLabs.Framework.Tests
{
    [ExcludeFromCodeCoverage]
    public sealed class CastTests
    {
        private readonly ICast _cast;

        public CastTests()
        {
            _cast = new Cast();
        }

        [Fact]
        public void ToTypeGeneric_ObjectArrayOfIntegersToIntEnumerable_Success()
        {
            var objectCollection = new object[]
            {
                1, 2, 3
            };
            var result = _cast.ToType<IEnumerable<int>>(objectCollection);

            Assert.NotNull(result);
            Assert.IsAssignableFrom<IEnumerable<int>>(result);
            Assert.Equal(objectCollection.Cast<int>(), result);
        }

        [Fact]
        public void ToTypeGeneric_ObjectArrayOfIntegersToIntReadOnlyCollection_Success()
        {
            var objectCollection = new object[]
            {
                1, 2, 3
            };
            var result = _cast.ToType<IReadOnlyCollection<int>>(objectCollection);
            
            Assert.NotNull(result);
            Assert.IsAssignableFrom<IReadOnlyCollection<int>>(result);
            Assert.Equal(objectCollection.Cast<int>(), result);
        }

        [Fact]
        public void ToTypeGeneric_ObjectArrayOfIntegersToIntArray_Success()
        {
            var objectCollection = new object[]
            {
                1, 2, 3
            };
            var result = _cast.ToType<int[]>(objectCollection);

            Assert.NotNull(result);
            Assert.IsAssignableFrom<int[]>(result);
            Assert.Equal(objectCollection.Cast<int>(), result);
        }

        [Fact]
        public void ToTypeGeneric_ObjectArrayOfMixedToIntEnumerableWithoutIterating_NoException()
        {
            var objectCollection = new object[]
            {
                1, "not an int", 3
            };

            var result = _cast.ToType<IEnumerable<int>>(objectCollection);

            Assert.NotNull(result);
            Assert.IsAssignableFrom<IEnumerable<int>>(result);
        }

        [Fact]
        public void ToTypeGeneric_ObjectArrayOfMixedToIntEnumerableWithIterating_ThrowsException()
        {
            var objectCollection = new object[]
            {
                1, "not an int", 3
            };

            var result = _cast.ToType<IEnumerable<int>>(objectCollection);
            Action forceIterationMethod = () => result.ToArray();

            Assert.Throws<InvalidCastException>(forceIterationMethod);
        }

        [Fact]
        public void ToTypeGeneric_ObjectArrayOfMixedToIntReadOnlyCollection_ThrowsException()
        {
            var objectCollection = new object[]
            {
                1, "not an int", 3
            };

            Action forceIterationMethod = () => _cast.ToType<IReadOnlyCollection<int>>(objectCollection);

            Assert.Throws<InvalidCastException>(forceIterationMethod);
        }

        [Fact]
        public void ToTypeGeneric_NullObject_ReturnsNull()
        {
            var result = _cast.ToType<object>(null);
            Assert.Null(result);
        }

        [InlineData("hey")]
        [InlineData((int)123)]
        [InlineData(123d)]
        [InlineData(123L)]
        [Theory]
        public void ToTypeGeneric_AssignableToObject_SameReference(object input)
        {
            var result = _cast.ToType<object>(input);
            Assert.NotNull(result);
            Assert.Same(input, result);
        }

        [Fact]
        public void ToTypeGeneric_LongAsObjectToLong_ReturnsLong()
        {
            var input = (object)123L;
            var result = _cast.ToType<long>(input);
            Assert.Equal((long)123, result);
        }

        [Fact]
        public void ToTypeGeneric_LongAsObjectToInt_ReturnsInt()
        {
            var input = (object)123L;
            var result = _cast.ToType<int>(input);
            Assert.Equal((int)123, result);
        }

        [Fact]
        public void ToTypeGeneric_LongAsObjectToShort_ReturnsShort()
        {
            var input = (object)123L;
            var result = _cast.ToType<short>(input);
            Assert.Equal((short)123, result);
        }

        [Fact]
        public void ToTypeGeneric_LongAsObjectToSByte_ReturnsSByte()
        {
            var input = (object)123L;
            var result = _cast.ToType<sbyte>(input);
            Assert.Equal((sbyte)123, result);
        }

        [Fact]
        public void ToTypeGeneric_LongAsObjectToULong_ReturnsLong()
        {
            var input = (object)123L;
            var result = _cast.ToType<ulong>(input);
            Assert.Equal((ulong)123, result);
        }

        [Fact]
        public void ToTypeGeneric_LongAsObjectToUInt_ReturnsUInt()
        {
            var input = (object)123L;
            var result = _cast.ToType<uint>(input);
            Assert.Equal((uint)123, result);
        }

        [Fact]
        public void ToTypeGeneric_LongAsObjectToUShort_ReturnsUShort()
        {
            var input = (object)123L;
            var result = _cast.ToType<ushort>(input);
            Assert.Equal((ushort)123, result);
        }

        [Fact]
        public void ToTypeGeneric_LongAsObjectToByte_ReturnsByte()
        {
            var input = (object)123L;
            var result = _cast.ToType<byte>(input);
            Assert.Equal((byte)123, result);
        }
    }
}
