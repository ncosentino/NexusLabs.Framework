using System;

using Autofac;
using Autofac.Core;

using Xunit;

namespace NexusLabs.Autofac.Tests.ContainerBuilderExtensions
{
    public sealed class WireTappingTests
    {
        [Fact]
        public void DefaultWireTap_SingleTapBaseSingleInstance_SameWireTapInstances()
        {
            var containerBuilder = new ContainerBuilder();
            containerBuilder
                .RegisterType<OriginalTestImplementation>()
                .SingleInstance()
                .AsImplementedInterfaces();
            containerBuilder.WireTap<ITestInterface>(baseInstance =>
            {
                Assert.IsType<OriginalTestImplementation>(baseInstance);
                return new WireTapTestImplementation();
            });

            using (var container = containerBuilder.Build())
            using (var scope = container.BeginLifetimeScope())
            {
                var instance1 = scope.Resolve<ITestInterface>();
                var instance2 = scope.Resolve<ITestInterface>();

                Assert.NotNull(instance1);
                Assert.IsType<WireTapTestImplementation>(instance1);
                Assert.NotNull(instance2);
                Assert.IsType<WireTapTestImplementation>(instance2);
                Assert.Equal(instance1, instance2);
            }
        }

        [Fact]
        public void SingleExplicitWireTap_SingleTapBaseSingleInstance_SameWireTapInstances()
        {
            var containerBuilder = new ContainerBuilder();
            containerBuilder
                .RegisterType<OriginalTestImplementation>()
                .SingleInstance()
                .AsImplementedInterfaces();
            containerBuilder.WireTap<ITestInterface>(baseInstance =>
            {
                Assert.IsType<OriginalTestImplementation>(baseInstance);
                return new WireTapTestImplementation();
            }, WireTapSingleInstance.SingleInstance);

            using (var container = containerBuilder.Build())
            using (var scope = container.BeginLifetimeScope())
            {
                var instance1 = scope.Resolve<ITestInterface>();
                var instance2 = scope.Resolve<ITestInterface>();

                Assert.NotNull(instance1);
                Assert.IsType<WireTapTestImplementation>(instance1);
                Assert.NotNull(instance2);
                Assert.IsType<WireTapTestImplementation>(instance2);
                Assert.Equal(instance1, instance2);
            }
        }

        [Fact]
        public void MultiExplicitWireTap_SingleTapBaseSingleInstance_ThrowsDependencyResolutionException()
        {
            var containerBuilder = new ContainerBuilder();
            containerBuilder
                .RegisterType<OriginalTestImplementation>()
                .SingleInstance()
                .AsImplementedInterfaces();
            containerBuilder.WireTap<ITestInterface>(baseInstance =>
            {
                Assert.IsType<OriginalTestImplementation>(baseInstance);
                return new WireTapTestImplementation();
            }, WireTapSingleInstance.NewInstances);

            using (var container = containerBuilder.Build())
            using (var scope = container.BeginLifetimeScope())
            {
                var actualException = Assert.Throws<DependencyResolutionException>(() => scope.Resolve<ITestInterface>());
                var innerException = actualException.InnerException;
                Assert.IsType<ArgumentException>(innerException);
                Assert.Equal(
                    $"The type to be wire-tapped is single " +
                    $"instance and after the first resolution " +
                    $"results in a wire-tap, the subsequent " +
                    $"resolutions will use the existing " +
                    $"instance. If you want new wire-tap " +
                    $"instances, it is suggested that you " +
                    $"change the registration of the type to " +
                    $"be wire-tapped to not be single instance.",
                    innerException.Message);
            }
        }

        [Fact]
        public void DefaultWireTap_SingleTapBaseMultiInstance_DifferentWireTapInstances()
        {
            var containerBuilder = new ContainerBuilder();
            containerBuilder
                .RegisterType<OriginalTestImplementation>()
                .AsImplementedInterfaces();
            containerBuilder.WireTap<ITestInterface>(baseInstance =>
            {
                Assert.IsType<OriginalTestImplementation>(baseInstance);
                return new WireTapTestImplementation();
            });

            using (var container = containerBuilder.Build())
            using (var scope = container.BeginLifetimeScope())
            {
                var instance1 = scope.Resolve<ITestInterface>();
                var instance2 = scope.Resolve<ITestInterface>();

                Assert.NotNull(instance1);
                Assert.IsType<WireTapTestImplementation>(instance1);
                Assert.NotNull(instance2);
                Assert.IsType<WireTapTestImplementation>(instance2);
                Assert.NotEqual(instance1, instance2);
            }
        }

        [Fact]
        public void SingleExplicitWireTap_SingleTapBasMultiInstance_DifferentWireTapInstances()
        {
            var containerBuilder = new ContainerBuilder();
            containerBuilder
                .RegisterType<OriginalTestImplementation>()
                .AsImplementedInterfaces();
            containerBuilder.WireTap<ITestInterface>(baseInstance =>
            {
                Assert.IsType<OriginalTestImplementation>(baseInstance);
                return new WireTapTestImplementation();
            }, WireTapSingleInstance.SingleInstance);

            using (var container = containerBuilder.Build())
            using (var scope = container.BeginLifetimeScope())
            {
                var instance1 = scope.Resolve<ITestInterface>();
                var instance2 = scope.Resolve<ITestInterface>();

                Assert.NotNull(instance1);
                Assert.IsType<WireTapTestImplementation>(instance1);
                Assert.NotNull(instance2);
                Assert.IsType<WireTapTestImplementation>(instance2);
                Assert.Equal(instance1, instance2);
            }
        }

        [Fact]
        public void MultiExplicitWireTap_SingleTapBaseMultiInstance_DifferentWireTapInstances()
        {
            var containerBuilder = new ContainerBuilder();
            containerBuilder
                .RegisterType<OriginalTestImplementation>()
                .AsImplementedInterfaces();
            containerBuilder.WireTap<ITestInterface>(baseInstance =>
            {
                Assert.IsType<OriginalTestImplementation>(baseInstance);
                return new WireTapTestImplementation();
            }, WireTapSingleInstance.NewInstances);

            using (var container = containerBuilder.Build())
            using (var scope = container.BeginLifetimeScope())
            {
                var instance1 = scope.Resolve<ITestInterface>();
                var instance2 = scope.Resolve<ITestInterface>();

                Assert.NotNull(instance1);
                Assert.IsType<WireTapTestImplementation>(instance1);
                Assert.NotNull(instance2);
                Assert.IsType<WireTapTestImplementation>(instance2);
                Assert.NotEqual(instance1, instance2);
            }
        }

        [Fact]
        public void DefaultWireTap_DoubleTapBaseSingleInstance_SameWireTapInstancesOfSecondWireTap()
        {
            var containerBuilder = new ContainerBuilder();
            containerBuilder
                .RegisterType<OriginalTestImplementation>()
                .SingleInstance()
                .AsImplementedInterfaces();
            containerBuilder.WireTap<ITestInterface>(baseInstance =>
            {
                Assert.IsType<OriginalTestImplementation>(baseInstance);
                return new WireTapTestImplementation();
            });
            containerBuilder.WireTap<ITestInterface>(baseInstance =>
            {
                Assert.IsType<WireTapTestImplementation>(baseInstance);
                return new WireTapTestImplementation2();
            });

            using (var container = containerBuilder.Build())
            using (var scope = container.BeginLifetimeScope())
            {
                var instance1 = scope.Resolve<ITestInterface>();
                var instance2 = scope.Resolve<ITestInterface>();

                Assert.NotNull(instance1);
                Assert.IsType<WireTapTestImplementation2>(instance1);
                Assert.NotNull(instance2);
                Assert.IsType<WireTapTestImplementation2>(instance2);
                Assert.Equal(instance1, instance2);
            }
        }

        [Fact]
        public void DefaultWireTap_DoubleTapBaseMultiInstance_DifferentWireTapInstancesOfSecondWireTap()
        {
            var containerBuilder = new ContainerBuilder();
            containerBuilder
                .RegisterType<OriginalTestImplementation>()
                .AsImplementedInterfaces();
            containerBuilder.WireTap<ITestInterface>(baseInstance =>
            {
                Assert.IsType<OriginalTestImplementation>(baseInstance);
                return new WireTapTestImplementation();
            });
            containerBuilder.WireTap<ITestInterface>(baseInstance =>
            {
                Assert.IsType<WireTapTestImplementation>(baseInstance);
                return new WireTapTestImplementation2();
            });

            using (var container = containerBuilder.Build())
            using (var scope = container.BeginLifetimeScope())
            {
                var instance1 = scope.Resolve<ITestInterface>();
                var instance2 = scope.Resolve<ITestInterface>();

                Assert.NotNull(instance1);
                Assert.IsType<WireTapTestImplementation2>(instance1);
                Assert.NotNull(instance2);
                Assert.IsType<WireTapTestImplementation2>(instance2);
                Assert.NotEqual(instance1, instance2);
            }
        }

        private interface ITestInterface
        {

        }

        private class OriginalTestImplementation : ITestInterface
        {

        }

        private class WireTapTestImplementation : ITestInterface
        {
        }

        private class WireTapTestImplementation2 : ITestInterface
        {
        }
    }
}
