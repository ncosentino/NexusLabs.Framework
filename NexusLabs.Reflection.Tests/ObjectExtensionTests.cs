using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xunit;

namespace NexusLabs.Reflection.Tests
{
    public sealed class ObjectExtensionTests
    {
        [Fact]
        private void SetField_ValidFieldName_DoesNotThrow()
        {
            var obj = new TestType();
            obj.SetField("_stringField", "expected value");
        }

        [Fact]
        private void SetField_ReadOnlyField_DoesNotThrow()
        {
            var obj = new TestType();
            obj.SetField("_readonlyStringField", "expected value");
        }

        [Fact]
        private void SetField_NullFieldName_ArgumentNullException()
        {
            var obj = new TestType();
            Action method = () => obj.SetField(null, "expected value");

            var exception = Assert.Throws<ArgumentNullException>(method);
            Assert.Equal("fieldName", exception.ParamName);
        }

        [Fact]
        private void SetField_InvalidFieldName_InvalidOperationException()
        {
            var obj = new TestType();
            Action method = () => obj.SetField("not an actual field", "expected value");

            Assert.Throws<InvalidOperationException>(method);
        }

        [Fact]
        private void GetField_ValidFieldName_ExpectedValue()
        {
            var obj = new TestType();
            obj.SetField("_stringField", "expected value");

            var result = obj.GetField("_stringField");

            Assert.Equal("expected value", result);
        }

        [Fact]
        private void GetField_ReadOnlyField_ExpectedValue()
        {
            var obj = new TestType();
            obj.SetField("_readonlyStringField", "expected value");

            var result = obj.GetField("_readonlyStringField");

            Assert.Equal("expected value", result);
        }

        private sealed class TestType
        {
            private readonly string _readonlyStringField;

            private string _stringField;
        }
    }
}
