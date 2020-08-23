using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xunit;

namespace NexusLabs.Reflection.Tests
{
    public sealed class TypeExtensionTests
    {
        [Fact]
        private void CreateInstance_PrivateConstructorNoParameters_CreatesInstance()
        {
            var result = typeof(PrivateConstructorType).CreateInstance(c => true);
            Assert.NotNull(result);
            Assert.IsType<PrivateConstructorType>(result);
        }

        [Fact]
        private void CreateInstance_PrivateConstructorStringParameter_CreatesInstanceWithParameter()
        {
            var result = typeof(PrivateConstructorType).CreateInstance(c =>
            {
                var parameters = c.GetParameters();
                return parameters.Length == 1 && parameters[0].ParameterType == typeof(string);
            },
            new object[] { "expected value" });

            Assert.NotNull(result);
            Assert.IsType<PrivateConstructorType>(result);
            Assert.Equal("expected value", result.GetField("_stringParameter"));
        }

        [Fact]
        private void CreateInstance_PrivateConstructorParameterTypeMismatch_ThrowsArgumentException()
        {
            Action method = () => typeof(PrivateConstructorType).CreateInstance(c =>
            {
                var parameters = c.GetParameters();
                return parameters.Length == 1 && parameters[0].ParameterType == typeof(string);
            },
            new object[] { 123 });

            Assert.Throws<ArgumentException>(method);
        }

        private sealed class PrivateConstructorType
        {
            private readonly string _stringParameter;

            private PrivateConstructorType()
            {
            }

            private PrivateConstructorType(string stringParameter)
            {
                _stringParameter = stringParameter;
            }
        }
    }
}
