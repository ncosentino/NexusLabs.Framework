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
        public void CreateDictionaryMembers_NoMembersProvided_Success()
        {
            var result = _dynamoFactory.Create<ITestInterface>(getters: null);

            Assert.NotNull(result);
            Assert.IsAssignableFrom<ITestInterface>(result);
        }

        [Fact]
        public void CreateDictionaryMembers_NoMembersProvided_ThrowOnAccess()
        {
            var result = _dynamoFactory.Create<ITestInterface>(getters: null);

            Action method = () => { var x = result.GetOnlyStringProperty; };

            Assert.Throws<RuntimeBinderException>(method);
        }

        [Fact]
        public void CreateDictionaryMembers_GetOnlyStringPropertyProvided_Success()
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
        public void CreateDictionaryMembers_GetSetStringPropertyProvided_UpdatesBackingField()
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

        public interface ITestInterface
        {
            string GetOnlyStringProperty { get; }

            string GetSetStringProperty { get; set; }
        }
    }
}
