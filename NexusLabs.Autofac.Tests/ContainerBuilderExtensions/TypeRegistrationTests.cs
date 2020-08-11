using System;
using System.Collections.Generic;
using System.Linq;

using Autofac;
using Autofac.Core;

using Xunit;

namespace NexusLabs.Autofac.Tests.ContainerBuilderExtensions
{
    public sealed class TypeRegistrationTests
    {
        [Fact]
        public void ResolveInstances_RegisterAssemblyTypesAsSingletonInterfaces_ExpectedSameServices()
        {
            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterAssemblyTypesAsSingletonInterfaces<ITestService>(GetType().Assembly);

            using (var container = containerBuilder.Build())
            using (var scope = container.BeginLifetimeScope())
            {
                var services1 = scope.Resolve<IEnumerable<ITestService>>().ToArray();
                var services2 = scope.Resolve<IEnumerable<ITestService>>().ToArray();

                Assert.Equal(2, services1.Length);
                Assert.IsType<Service1>(services1[0]);
                Assert.IsType<Service2>(services1[1]);

                Assert.Equal(2, services2.Length);
                Assert.IsType<Service1>(services2[0]);
                Assert.IsType<Service2>(services2[1]);

                Assert.Equal(services1[0], services2[0]);
                Assert.Equal(services1[1], services2[1]);
            }
        }

        private interface ITestService
        {

        }

        private class Service1 : ITestService
        {

        }

        private class Service2 : ITestService
        {
        }

        private class NotAService
        {
        }
    }
}
