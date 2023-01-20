using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

using Autofac;
using Autofac.Core;

using Xunit;

namespace NexusLabs.Autofac.Tests.ContainerBuilderExtensions
{
    [ExcludeFromCodeCoverage]
    public sealed class FacadeTests
    {
        [Fact]
        public void Resolve_ValidDependenciesIntoReadOnlyCollectionSource_FacadeWithSources()
        {
            var container = CreateContainerForFacade<FacadeWithDependencyAndRegisteredIReadOnlyCollectionSource>();

            var service = container.Resolve<ITestService>();
            Assert.NotNull(service);
            Assert.Equal(typeof(FacadeWithDependencyAndRegisteredIReadOnlyCollectionSource), service.GetType());

            var facade= (FacadeWithDependencyAndRegisteredIReadOnlyCollectionSource)service;
            Assert.Equal(2, facade.Services.Count);
        }

        [Fact]
        public void Resolve_ValidDependenciesIntoEnumerableSource_FacadeWithSources()
        {
            var container = CreateContainerForFacade<FacadeWithDependencyAndRegisteredIEnumerableSource>();

            var service = container.Resolve<ITestService>();
            Assert.NotNull(service);
            Assert.Equal(typeof(FacadeWithDependencyAndRegisteredIEnumerableSource), service.GetType());

            var facade = (FacadeWithDependencyAndRegisteredIEnumerableSource)service;
            Assert.Equal(2, facade.Services.Count);
        }

        // this doesn't seem totally invalid to support, but I can't see a
        // good reason to allow it given that it's a 1:1 wrapper...
        [Fact]
        public void Resolve_FacadeWithSingleSource_Throws()
        {
            var exception = Assert.Throws<InvalidOperationException>(() => CreateContainerForFacade<FacadeWithSingleSource>());
        }

        [Fact]
        public void Resolve_FacadeWithNoSource_Throws()
        {
            var exception = Assert.Throws<InvalidOperationException>(() => CreateContainerForFacade<FacadeWithNoSource>());
        }

        private static IContainer CreateContainerForFacade<TFacade>()
            where TFacade : ITestService
        {
            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterDiscoverableTypes(Assembly.GetExecutingAssembly());
            containerBuilder
                .RegisterType<SomeDependency>()
                .SingleInstance();
            containerBuilder
                .RegisterFacadeWithDiscoverableSources<TFacade, ITestService>()
                .SingleInstance()
                .AsImplementedInterfaces();
            var container = containerBuilder.Build();
            return container;
        }

        private interface ITestService
        {
        }

        [DiscoverableForRegistration(DiscoveryRegistrationMode.ImplementedInterfaces, true)]
        private sealed class ImplementationA : ITestService
        {
        }

        [DiscoverableForRegistration(DiscoveryRegistrationMode.ImplementedInterfaces, true)]
        private sealed class ImplementationB : ITestService
        {
        }

        private class SomeDependency
        {
        }

        private class FacadeWithDependencyAndRegisteredIReadOnlyCollectionSource : ITestService
        { 
            public FacadeWithDependencyAndRegisteredIReadOnlyCollectionSource(
                SomeDependency someDependency,
                IReadOnlyCollection<ITestService> services)
            {
                Services = services;
            }

            public IReadOnlyCollection<ITestService> Services { get; }
        }

        private class FacadeWithDependencyAndRegisteredIEnumerableSource : ITestService
        {
            public FacadeWithDependencyAndRegisteredIEnumerableSource(
                SomeDependency someDependency,
                IEnumerable<ITestService> services)
            {
                Services = services.ToArray();
            }

            public IReadOnlyCollection<ITestService> Services { get; }
        }

        private class FacadeWithSingleSource : ITestService
        {
            public FacadeWithSingleSource(
                SomeDependency someDependency,
                ITestService service)
            {
            }
        }

        private class FacadeWithNoSource : ITestService
        {
            public FacadeWithNoSource(
                SomeDependency someDependency)
            {
            }
        }
    }
}
