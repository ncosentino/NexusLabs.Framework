using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xunit;

namespace NexusLabs.Reflection.Tests
{
    public sealed class AssemblyExtensionTests
    {
        [Fact]
        private void GetNestedType_PrivateNestedType_NotNull()
        {
            var type = typeof(ParentType)
                .Assembly
                .GetNestedType(
                    typeof(ParentType).FullName,
                    "PrivateNestedType");
            Assert.NotNull(type);
        }

        [Fact]
        private void GetNestedType_InvalidNestedType_Null()
        {
            var type = typeof(ParentType)
                .Assembly
                .GetNestedType(
                    typeof(ParentType).FullName,
                    "not a real boi");
            Assert.Null(type);
        }

        [Fact]
        private void GetNestedType_FullNestedName_NotNull()
        {
            var fullNestedTypeIdentifier = $"{typeof(ParentType).FullName}+PrivateNestedType";
            var type = typeof(ParentType)
                .Assembly
                .GetNestedType(fullNestedTypeIdentifier);
            Assert.NotNull(type);
            Assert.Equal("PrivateNestedType", type.Name);
        }

        [Fact]
        private void GetNestedType_FullNestedNameDoubleNested_NotNull()
        {
            var fullNestedTypeIdentifier = $"{typeof(ParentType).FullName}+PrivateNestedType+PrivateDoubleNestedType";
            var type = typeof(ParentType)
                .Assembly
                .GetNestedType(fullNestedTypeIdentifier);
            Assert.NotNull(type);
            Assert.Equal("PrivateDoubleNestedType", type.Name);
        }

        private sealed class ParentType
        {
            private sealed class PrivateNestedType
            {
                private sealed class PrivateDoubleNestedType
                {
                }
            }
        }
    }
}
