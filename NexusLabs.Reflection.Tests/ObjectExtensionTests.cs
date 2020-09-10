using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

        [Fact]
        private void RaiseEvent_EventDoesntExist_ThrowsInvalidOperationException()
        {
            var obj = new TestType();
            Action method = () => obj.RaiseEvent("not an actual event", new CustomEventArgs());

            Assert.Throws<InvalidOperationException>(method);
        }

        [Fact]
        private void RaiseEvent_EventArgumentTypeMismatch_ThrowsArgumentException()
        {
            var obj = new TestType();
            obj.PublicEvent += (_, __) => { };

            Action method = () => obj.RaiseEvent(nameof(TestType.PublicEvent), EventArgs.Empty);

            Assert.Throws<ArgumentException>(method);
        }

        [Fact]
        private void RaiseEvent_NoHandler_DoesNotThrow()
        {
            var eventArgs = new CustomEventArgs();
            var obj = new TestType();

            obj.RaiseEvent(nameof(TestType.PublicEvent), eventArgs);
        }

        [Fact]
        private void RaiseEvent_SingleHandler_RaisedExpectedCount()
        {
            var eventArgs = new CustomEventArgs();
            var obj = new TestType();

            var count = 0;
            obj.PublicEvent += (s, e) =>
            {
                Assert.Equal(obj, s);
                Assert.Equal(eventArgs, e);
                count++;
            };

            obj.RaiseEvent(nameof(TestType.PublicEvent), eventArgs);

            Assert.Equal(1, count);
        }

        [Fact]
        private void RaiseEvent_SingleHandlerAlternateSender_RaisedExpectedCount()
        {
            var sender = new object();
            var eventArgs = new CustomEventArgs();
            var obj = new TestType();

            var count = 0;
            obj.PublicEvent += (s, e) =>
            {
                Assert.Equal(sender, s);
                Assert.Equal(eventArgs, e);
                count++;
            };

            obj.RaiseEvent(nameof(TestType.PublicEvent), sender, eventArgs);

            Assert.Equal(1, count);
        }

        [Fact]
        private void RaiseEvent_MultipleHandlers_AllHandlersExecuted()
        {
            var eventArgs = new CustomEventArgs();
            var obj = new TestType();

            var count1 = 0;
            obj.PublicEvent += (s, e) =>
            {
                Assert.Equal(obj, s);
                Assert.Equal(eventArgs, e);
                count1++;
            };

            var count2 = 0;
            obj.PublicEvent += (s, e) =>
            {
                Assert.Equal(obj, s);
                Assert.Equal(eventArgs, e);
                count2++;
            };

            obj.RaiseEvent(nameof(TestType.PublicEvent), eventArgs);

            Assert.Equal(1, count1);
            Assert.Equal(1, count2);
        }

        [Fact]
        private void RaiseEvent_FirstHandlerThrows_HandlerChainEndsExceptionRaised()
        {
            var eventArgs = new CustomEventArgs();
            var obj = new TestType();

            var expectedException = new InvalidOperationException("expected");
            var count1 = 0;
            obj.PublicEvent += (s, e) =>
            {
                Assert.Equal(obj, s);
                Assert.Equal(eventArgs, e);
                count1++;
                throw expectedException;
            };

            var count2 = 0;
            obj.PublicEvent += (s, e) =>
            {
                Assert.Equal(obj, s);
                Assert.Equal(eventArgs, e);
                count2++;
            };

            Action method = () => obj.RaiseEvent(nameof(TestType.PublicEvent), eventArgs);

            var actualException = Assert.Throws<TargetInvocationException>(method);

            Assert.Equal(expectedException, actualException.InnerException);
            Assert.Equal(1, count1);
            Assert.Equal(0, count2);
        }

        private sealed class TestType
        {
            private readonly string _readonlyStringField;

            private string _stringField;

            public event EventHandler<CustomEventArgs> PublicEvent;
        }

        private sealed class CustomEventArgs : EventArgs
        {
        }
    }
}
