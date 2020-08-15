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
    }
}
