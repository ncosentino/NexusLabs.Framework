using System;
using System.Linq;

using Autofac;
using Autofac.Core;
using Autofac.Core.Registration;

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
        public void DefaultWireTap_SingleTapBaseSingleInstanceLazyResolution_SameWireTapInstances()
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
                var instance1 = scope.Resolve<Lazy<ITestInterface>>();
                var instance2 = scope.Resolve<Lazy<ITestInterface>>();

                Assert.NotNull(instance1.Value);
                Assert.IsType<WireTapTestImplementation>(instance1.Value);
                Assert.NotNull(instance2.Value);
                Assert.IsType<WireTapTestImplementation>(instance2.Value);
                Assert.Equal(instance1.Value, instance2.Value);
            }
        }

        [Fact]
        public void DefaultWireTap_IncompatibleTypes_ThrowsDependencyResolutionExceptionWithVerboseInnerException()
        {
            var containerBuilder = new ContainerBuilder();
            containerBuilder
                .RegisterType<OriginalTestImplementation>()
                .SingleInstance()
                .AsImplementedInterfaces();
            containerBuilder.WireTap<ITestInterface>(baseInstance =>
            {
                Assert.IsType<OriginalTestImplementation>(baseInstance);
                return new object();
            });

            using (var container = containerBuilder.Build())
            using (var scope = container.BeginLifetimeScope())
            {
                var dependencyResolutionException = Assert.Throws<DependencyResolutionException>(() => scope.Resolve<ITestInterface>());
                Assert.NotNull(dependencyResolutionException.InnerException);
                var innerException = Assert.IsType<InvalidOperationException>(dependencyResolutionException.InnerException);
                Assert.Equal(
                    $"The wire tap result is not assignable to the type " +
                    $"that is being wire tapped. The result of the wire " +
                    $"tap was of type '{typeof(object).FullName}' " +
                    $"but the type being wire tapped is of type " +
                    $"'{typeof(ITestInterface).FullName}'.",
                    innerException.Message);
            }
        }

        [Fact]
        public void DefaultWireTap_NullWireTapResult_ThrowsDependencyResolutionExceptionWithVerboseInnerException()
        {
            var containerBuilder = new ContainerBuilder();
            containerBuilder
                .RegisterType<OriginalTestImplementation>()
                .SingleInstance()
                .AsImplementedInterfaces();
            containerBuilder.WireTap<ITestInterface>(baseInstance =>
            {
                Assert.IsType<OriginalTestImplementation>(baseInstance);
                return null;
            });

            using (var container = containerBuilder.Build())
            using (var scope = container.BeginLifetimeScope())
            {
                var dependencyResolutionException = Assert.Throws<DependencyResolutionException>(() => scope.Resolve<ITestInterface>());
                Assert.NotNull(dependencyResolutionException.InnerException);
                var innerException = Assert.IsType<InvalidOperationException>(dependencyResolutionException.InnerException);
                Assert.Equal(
                    $"Cannot return null from a wire tap for type " +
                    $"'{typeof(ITestInterface).FullName}' because Autofac does not " +
                    $"allow null registrations.",
                    innerException.Message);
            }
        }

        [Fact]
        public void DefaultWireTap_SingleTapMultipleUnrelatedRegistrations_WireTapCallbackRunOnce()
        {
            var containerBuilder = new ContainerBuilder();
            containerBuilder
                .RegisterType<OriginalTestImplementation>()
                .SingleInstance()
                .AsImplementedInterfaces();
            containerBuilder
                .RegisterType<SomeOtherDependency>()
                .SingleInstance();
            containerBuilder
                .RegisterType<SomeOtherDependency2>()
                .SingleInstance();

            int wireTapCount = 0;
            containerBuilder.WireTap<ITestInterface>(baseInstance =>
            {
                wireTapCount++;
                Assert.IsType<OriginalTestImplementation>(baseInstance);
                return new WireTapTestImplementation();
            });

            using (var container = containerBuilder.Build())
            using (var scope = container.BeginLifetimeScope())
            {
                var instance1 = scope.Resolve<ITestInterface>();
                var instance2 = scope.Resolve<ITestInterface>();

                Assert.Equal(1, wireTapCount);
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


        [Fact]
        public void DefaultThrowOnNotRegisteredWireTap_NoRegisteredTypeToWireTap_ThrowInvalidOperationForNoTypeRegistered()
        {
            var containerBuilder = new ContainerBuilder();

            // NOTE: explicitly do not register this :)
            //containerBuilder
            //    .RegisterType<OriginalTestImplementation>()
            //    .SingleInstance()
            //    .AsImplementedInterfaces();

            containerBuilder.WireTap<ITestInterface>(baseInstance =>
            {
                Assert.IsType<OriginalTestImplementation>(baseInstance);
                return new WireTapTestImplementation();
            });

            var exception = Assert.Throws<InvalidOperationException>(() => containerBuilder.Build());
            Assert.Equal(
                $"Could not create a wire tap for type '{typeof(ITestInterface).FullName}' " +
                $"because there is no typed service for the specified type.",
                exception.Message);
        }

        [Fact]
        public void ExplicitNoThrowOnErrorWireTap_NoRegisteredTypeToWireTap_NoWireTapExceptionThrowsResolveException()
        {
            var containerBuilder = new ContainerBuilder();

            // NOTE: explicitly do not register this :)
            //containerBuilder
            //    .RegisterType<OriginalTestImplementation>()
            //    .SingleInstance()
            //    .AsImplementedInterfaces();

            containerBuilder.WireTap<ITestInterface>(baseInstance =>
            {
                Assert.IsType<OriginalTestImplementation>(baseInstance);
                return new WireTapTestImplementation();
            },
            false);

            using (var container = containerBuilder.Build())
            using (var scope = container.BeginLifetimeScope())
            {
                var exception = Assert.Throws<ComponentNotRegisteredException>(() => scope.Resolve<ITestInterface>());
                Assert.Equal(
                    $"The requested service '{typeof(ITestInterface).FullName}' " +
                    $"has not been registered. To avoid this exception, either " +
                    $"register a component to provide the service, check for " +
                    $"service registration using IsRegistered(), or use the " +
                    $"ResolveOptional() method to resolve an optional " +
                    $"dependency.",
                    exception.Message);
            }
        }

        [Fact]
        public void DefaultThrowNotRegisteredWireTap_MultipleWireTapsWithoutRegisteredTypes_ThrowInvalidOperationForNoTypeRegistered()
        {
            var containerBuilder = new ContainerBuilder();

            containerBuilder
                .RegisterType<OriginalTestImplementation>()
                .SingleInstance()
                .AsImplementedInterfaces();
            containerBuilder
                .RegisterType<OriginalTestImplementation>()
                .SingleInstance()
                .AsImplementedInterfaces();

            containerBuilder.WireTap<WireTapTestImplementation>(baseInstance =>
            {
                Assert.True(false, $"This wire tap should not run.");
                return new WireTapTestImplementation2();
            });
            containerBuilder.WireTap<WireTapTestImplementation>(baseInstance =>
            {
                Assert.True(false, $"This wire tap should not run.");
                return new WireTapTestImplementation2();
            });
            containerBuilder.WireTap<WireTapTestImplementation>(baseInstance =>
            {
                Assert.True(false, $"This wire tap should not run.");
                return new WireTapTestImplementation2();
            });
            containerBuilder.WireTap<WireTapTestImplementation>(baseInstance =>
            {
                Assert.True(false, $"This wire tap should not run.");
                return new WireTapTestImplementation2();
            });

            var exception = Assert.Throws<InvalidOperationException>(() => containerBuilder.Build());
            Assert.Equal(
                $"Could not create a wire tap for type '{typeof(WireTapTestImplementation).FullName}' " +
                $"because there is no typed service for the specified type.",
                exception.Message);
        }

        [Fact]
        public void OnlyOneDefaultThrowNotRegisteredWireTap_MultipleWireTapsWithoutRegisteredTypes_ThrowInvalidOperationForNoTypeRegistered()
        {
            var containerBuilder = new ContainerBuilder();

            containerBuilder
                .RegisterType<OriginalTestImplementation>()
                .SingleInstance()
                .AsImplementedInterfaces();
            containerBuilder
                .RegisterType<OriginalTestImplementation>()
                .SingleInstance()
                .AsImplementedInterfaces();

            containerBuilder.WireTap<WireTapTestImplementation>(baseInstance =>
            {
                Assert.True(false, $"This wire tap should not run.");
                return new WireTapTestImplementation2();
            }, false);
            containerBuilder.WireTap<WireTapTestImplementation>(baseInstance =>
            {
                Assert.True(false, $"This wire tap should not run.");
                return new WireTapTestImplementation2();
            }); // LOOK! Someone left this as the detault, which is to throw... so this will cause explosion.
            containerBuilder.WireTap<WireTapTestImplementation>(baseInstance =>
            {
                Assert.True(false, $"This wire tap should not run.");
                return new WireTapTestImplementation2();
            }, false);
            containerBuilder.WireTap<WireTapTestImplementation>(baseInstance =>
            {
                Assert.True(false, $"This wire tap should not run.");
                return new WireTapTestImplementation2();
            }, false);

            var exception = Assert.Throws<InvalidOperationException>(() => containerBuilder.Build());
            Assert.Equal(
                $"Could not create a wire tap for type '{typeof(WireTapTestImplementation).FullName}' " +
                $"because there is no typed service for the specified type.",
                exception.Message);
        }

        [Fact]
        public void DefaultWireTap_MultipleWireTapsWithoutRegisteredTypesExplicitNoThrowOnError_WireTapCallbacksNeverRun()
        {
            var containerBuilder = new ContainerBuilder();

            containerBuilder
                .RegisterType<OriginalTestImplementation>()
                .SingleInstance()
                .AsImplementedInterfaces();
            containerBuilder
                .RegisterType<OriginalTestImplementation>()
                .SingleInstance()
                .AsImplementedInterfaces();

            containerBuilder.WireTap<WireTapTestImplementation>(baseInstance =>
            {
                Assert.True(false, $"This wire tap should not run.");
                return new WireTapTestImplementation2();
            }, false);
            containerBuilder.WireTap<WireTapTestImplementation>(baseInstance =>
            {
                Assert.True(false, $"This wire tap should not run.");
                return new WireTapTestImplementation2();
            }, false);
            containerBuilder.WireTap<WireTapTestImplementation>(baseInstance =>
            {
                Assert.True(false, $"This wire tap should not run.");
                return new WireTapTestImplementation2();
            }, false);
            containerBuilder.WireTap<WireTapTestImplementation>(baseInstance =>
            {
                Assert.True(false, $"This wire tap should not run.");
                return new WireTapTestImplementation2();
            }, false);

            using (var container = containerBuilder.Build())
            using (var scope = container.BeginLifetimeScope())
            {
                var instance = scope.Resolve<ITestInterface>();
            }
        }

        [Fact]
        public void DefaultWireTap_NotSingleInstanceBaseWithMultipleInterfaceRegistrationsAndOneTapPerInterface_EachWireTapRunsSuccessfully()
        {
            var containerBuilder = new ContainerBuilder();

            containerBuilder
                .RegisterType<TestMultiInterfaceImplementation>()
                //.SingleInstance()
                .AsImplementedInterfaces();

            containerBuilder.WireTap<ITestMultiInterfaceBase1>(baseInstance =>
            {
                Assert.IsType<TestMultiInterfaceImplementation>(baseInstance);
                return new WireTapTestMultiInterfaceImplementation();
            });
            containerBuilder.WireTap<ITestMultiInterfaceBase2>(baseInstance =>
            {
                Assert.IsType<TestMultiInterfaceImplementation>(baseInstance);
                return new WireTapTestMultiInterfaceImplementation2();
            });

            using (var container = containerBuilder.Build())
            using (var scope = container.BeginLifetimeScope())
            {
                var instance1 = scope.Resolve<ITestMultiInterfaceBase1>();
                var instance2 = scope.Resolve<ITestMultiInterfaceBase2>();

                Assert.NotNull(instance1);
                Assert.IsType<WireTapTestMultiInterfaceImplementation>(instance1);
                Assert.NotNull(instance2);
                Assert.IsType<WireTapTestMultiInterfaceImplementation2>(instance2);
            }
        }

        // FIXME: please make this work some day
        [Fact]
        public void DefaultWireTap_SingleInstanceBaseWithMultipleInterfaceRegistrationsAndOneTapPerInterface_ThrowsInvalidOperationException()
        {
            var containerBuilder = new ContainerBuilder();

            containerBuilder
                .RegisterType<TestMultiInterfaceImplementation>()
                .SingleInstance() // NOTE: this causes us some grief because it doesn't allow the second wire tap to run
                .AsImplementedInterfaces();

            containerBuilder.WireTap<ITestMultiInterfaceBase1>(baseInstance =>
            {
                Assert.IsType<TestMultiInterfaceImplementation>(baseInstance);
                return new WireTapTestMultiInterfaceImplementation();
            });
            containerBuilder.WireTap<ITestMultiInterfaceBase2>(baseInstance =>
            {
                Assert.IsType<TestMultiInterfaceImplementation>(baseInstance);
                return new WireTapTestMultiInterfaceImplementation2();
            });

            var exception = Assert.Throws<InvalidOperationException>(() => containerBuilder.Build());
            Assert.Equal(
                $"A (currently) unsupported registration+wire " +
                $"tapping combination has been detected. The type " +
                $"to wire tap '{typeof(TestMultiInterfaceImplementation).FullName} " +
                $"has been registered as SingleInstance and there " +
                $"are unique wire taps registered for the " +
                $"following 'services' associated with it: " +
                $"{string.Join(", ", new Type[] { typeof(ITestMultiInterfaceBase1), typeof(ITestMultiInterfaceBase2) }.Select(st => $"'{st.FullName}'"))}." +
                $"\r\n" +
                $"It does not (yet) seem possible to override the " +
                $"behavior to get the cached single instance that " +
                $"Autofac will resolve, so the following wire taps " +
                $"will never run as they are expected to.",
                exception.Message);
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

        private class SomeOtherDependency
        {
        }

        private class SomeOtherDependency2
        {
        }

        private interface ITestMultiInterfaceBase1
        {
        }

        private interface ITestMultiInterfaceBase2
        {
        }

        private class TestMultiInterfaceImplementation : ITestMultiInterfaceBase1, ITestMultiInterfaceBase2
        {
        }

        private class WireTapTestMultiInterfaceImplementation : ITestMultiInterfaceBase1, ITestMultiInterfaceBase2
        {
        }

        private class WireTapTestMultiInterfaceImplementation2 : ITestMultiInterfaceBase1, ITestMultiInterfaceBase2
        {
        }
    }
}
