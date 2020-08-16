using System.Diagnostics.CodeAnalysis;

using NexusLabs.Dynamo.Properties;

using Xunit;

namespace NexusLabs.Dynamo.Tests
{
    [ExcludeFromCodeCoverage]
    public sealed class NotifyChangedDynamoPropertyTests
    {
        [Fact]
        public void StringInvokeSetter_DefaultChangeHandlerNewValue_EventRaisedBackingValueChanged()
        {
            var property = new NotifyChangedDynamoProperty<string>();

            var changedCount = 0;
            property.Changed += (s, e) => changedCount++;

            property.Setter.Invoke("property name", "new value");

            Assert.Equal(1, changedCount);
            Assert.Equal("new value", property.Getter.Invoke("property name"));
        }

        [Fact]
        public void StringInvokeSetter_DefaultChangeHandlerSameValue_EventNotRaisedBackingValueRemains()
        {
            var property = new NotifyChangedDynamoProperty<string>();
            property.Setter.Invoke("property name", "same value");

            var changedCount = 0;
            property.Changed += (s, e) => changedCount++;

            property.Setter.Invoke("property name", "same value");

            Assert.Equal(0, changedCount);
            Assert.Equal("same value", property.Getter.Invoke("property name"));
        }

        [Fact]
        public void IntInvokeSetter_DefaultChangeHandlerNewValue_EventRaisedBackingValueChanged()
        {
            var property = new NotifyChangedDynamoProperty<int>();

            var changedCount = 0;
            property.Changed += (s, e) => changedCount++;

            property.Setter.Invoke("property name", 123);

            Assert.Equal(1, changedCount);
            Assert.Equal(123, property.Getter.Invoke("property name"));
        }

        [Fact]
        public void IntInvokeSetter_DefaultChangeHandlerSameValue_EventNotRaisedBackingValueRemains()
        {
            var property = new NotifyChangedDynamoProperty<int>();
            property.Setter.Invoke("property name", 123);

            var changedCount = 0;
            property.Changed += (s, e) => changedCount++;

            property.Setter.Invoke("property name", 123);

            Assert.Equal(0, changedCount);
            Assert.Equal(123, property.Getter.Invoke("property name"));
        }

        [Fact]
        public void ObjectInvokeSetter_DefaultChangeHandlerNewValue_EventRaisedBackingValueChanged()
        {
            var property = new NotifyChangedDynamoProperty<object>();

            var changedCount = 0;
            property.Changed += (s, e) => changedCount++;

            var expectedObject = new object();
            property.Setter.Invoke("property name", expectedObject);

            Assert.Equal(1, changedCount);
            Assert.Equal(expectedObject, property.Getter.Invoke("property name"));
        }

        [Fact]
        public void ObjectInvokeSetter_DefaultChangeHandlerSameValue_EventNotRaisedBackingValueRemains()
        {
            var expectedObject = new object();

            var property = new NotifyChangedDynamoProperty<object>();
            property.Setter.Invoke("property name", expectedObject);

            var changedCount = 0;
            property.Changed += (s, e) => changedCount++;

            property.Setter.Invoke("property name", expectedObject);

            Assert.Equal(0, changedCount);
            Assert.Equal(expectedObject, property.Getter.Invoke("property name"));
        }
    }
}
