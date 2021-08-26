using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using Microsoft.CSharp.RuntimeBinder;

using NexusLabs.Dynamo.Properties;

using Xunit;

namespace NexusLabs.Dynamo.Tests
{
    [ExcludeFromCodeCoverage]
    public sealed class DynamoFactoryTests
    {
        private readonly IDynamoFactory _dynamoFactory;

        public DynamoFactoryTests()
        {
            _dynamoFactory = new DynamoFactory();
        }

        [Fact]
        public void CreateInterfaceWithDictionaryMembers_NoMembersProvided_Success()
        {
            var result = _dynamoFactory.Create<ITestInterface>(getters: null);

            Assert.NotNull(result);
            Assert.IsAssignableFrom<ITestInterface>(result);
        }

        [Fact]
        public void CreateInterfaceWithDictionaryMembers_NoMembersProvided_ThrowOnAccess()
        {
            var result = _dynamoFactory.Create<ITestInterface>(getters: null);

            Action method = () => { var x = result.GetOnlyStringProperty; };

            Assert.Throws<RuntimeBinderException>(method);
        }

        [Fact]
        public void CreateInterfaceWithDictionaryMembers_GetOnlyStringPropertyProvided_Success()
        {
            var result = _dynamoFactory.Create<ITestInterface>(
                getters: new Dictionary<string, DynamoGetterDelegate>()
                {
                    [nameof(ITestInterface.GetOnlyStringProperty)] = _ => "expected string",
                },
                setters: new Dictionary<string, DynamoSetterDelegate>());

            Assert.Equal("expected string", result.GetOnlyStringProperty);
        }

        [Fact]
        public void CreateInterfaceWithDictionaryMembers_LooseCreationAccessNotProvidedProperty_DefaultValue()
        {
            var result = _dynamoFactory.Create<ITestInterface>(
                getters: new Dictionary<string, DynamoGetterDelegate>()
                {
                    // explicitly do not define this since we'll try accessing it later
                    //[nameof(ITestInterface.GetOnlyStringProperty)] = _ => "expected string",
                },
                setters: new Dictionary<string, DynamoSetterDelegate>(),
                strict: false);

            Assert.Equal(default, result.GetOnlyStringProperty);
        }

        [Fact]
        public void CreateInterfaceWithDictionaryMembers_SpecifiedMethodNoArgs_ExpectedValue()
        {
            var result = _dynamoFactory.Create<ITestInterface>(
                methods: new Dictionary<string, DynamoInvokableDelegate>()
                {
                    [nameof(ITestInterface.MethodWithStringReturnValue)] = (_, __) => "this is the expected value",
                });

            Assert.Equal("this is the expected value", result.MethodWithStringReturnValue());
        }

        [Fact]
        public void CreateInterfaceWithDictionaryMembers_LooseCreationNotProvidedMethod_DefaultValue()
        {
            var result = _dynamoFactory.Create<ITestInterface>(
                methods: new Dictionary<string, DynamoInvokableDelegate>()
                {
                    // explicitly do not define this since we'll try accessing it later
                    //[nameof(ITestInterface.MethodWithStringReturnValue)] = (_, __) => "this is the expected value",
                },
                strict: false);

            Assert.Equal(default, result.MethodWithStringReturnValue());
        }

        [Fact]
        public void CreateInterfaceWithDictionaryMembers_GetSetStringPropertyProvided_UpdatesBackingField()
        {
            var storage = "original value";
            var result = _dynamoFactory.Create<ITestInterface>(
                properties: new Dictionary<string, IDynamoProperty>()
                {
                    [nameof(ITestInterface.GetSetStringProperty)] = new DynamoProperty(
                        _ => storage,
                        (_, value) => storage = (string)value),
                });

            result.GetSetStringProperty = "expected string";
            Assert.Equal("expected string", result.GetSetStringProperty);
            Assert.Equal("expected string", storage);
        }

        [Fact]
        public void CreateAbstractWithDictionaryMembers_VirtualGetSetStringPropertyProvided_UpdatesBackingField()
        {
            var storage = "original value";
            var result = _dynamoFactory.Create<TestAbstractClass>(
                properties: new Dictionary<string, IDynamoProperty>()
                {
                    [nameof(TestAbstractClass.VirtualGetSetStringProperty)] = new DynamoProperty(
                        _ => storage,
                        (_, value) => storage = (string)value),
                });

            result.VirtualGetSetStringProperty = "expected string";
            Assert.Equal("expected string", result.VirtualGetSetStringProperty);
            Assert.Equal("expected string", storage);
        }

        [Fact]
        public void CreateAbstractWithDictionaryMembers_NonVirtualGetOnlyStringPropertyProvided_ThrowsArgumentException()
        {
            Action method = () => _dynamoFactory.Create<TestAbstractClass>(
                properties: new Dictionary<string, IDynamoProperty>()
                {
                    [nameof(TestAbstractClass.GetOnlyStringProperty)] = new SimpleDynamoProperty<string>("expected string"),
                });
            Assert.Throws<ArgumentException>(method);
        }

        [Fact]
        public void CreatePrivateConstructorClass_NoProperties_ThrowsNotSupportedException()
        {
            Action method = () => _dynamoFactory.Create<PrivateConstructorTestClass>(properties: null);
            Assert.Throws<NotSupportedException>(method);
        }

        [Fact]
        public void CreateSealedClass_NoProperties_ThrowsNotSupportedException()
        {
            Action method = () => _dynamoFactory.Create<SealedTestClass>(properties: null);
            Assert.Throws<NotSupportedException>(method);
        }

        public interface ITestInterface
        {
            string GetOnlyStringProperty { get; }

            string GetSetStringProperty { get; set; }

            string MethodWithStringReturnValue();
        }

        public abstract class TestAbstractClass
        {
            public string GetOnlyStringProperty { get; }

            public virtual string VirtualGetSetStringProperty { get; set; }

            public string MethodWithStringReturnValue() => "test return value";
        }

        public class PrivateConstructorTestClass
        {
            private PrivateConstructorTestClass()
            {
            }

            public string GetOnlyStringProperty { get; }

            public string GetSetStringProperty { get; set; }
        }

        public sealed class SealedTestClass
        {
            private SealedTestClass()
            {
            }

            public string GetOnlyStringProperty { get; }

            public string GetSetStringProperty { get; set; }
        }
    }
}
