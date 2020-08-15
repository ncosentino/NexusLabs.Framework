using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
