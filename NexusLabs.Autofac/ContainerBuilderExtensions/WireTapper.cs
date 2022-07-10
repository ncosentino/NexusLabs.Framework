using System;
using System.Collections.Generic;
using System.Linq;

using Autofac.Core;
using Autofac.Core.Resolving.Pipeline;

namespace Autofac
{
    /// <summary>
    /// Provides an implementation for wire tapping into Autofac registrations 
    /// on a <see cref="ContainerBuilder"/>. This allows callers to intercept 
    /// instances of types during resolution and potentially return an entirely 
    /// new instance.
    /// </summary>
    public sealed class WireTapper
    {
        private const WireTapSingleInstance DEFAULT_SINGLE_INSTANCE = WireTapSingleInstance.Inherit;
        private const bool DEFAULT_THROW_ON_NO_REGISTERED_TYPE_TO_WIRE_TAP = true;
        private readonly ContainerBuilder _containerBuilder;

        private delegate object WireTapFactory(
            IComponentContext componentContext,
            object instance);

        private readonly Dictionary<Type, List<WireTapFactoryRegistration>> _wireTapFactoryRegistrations;
        private bool _registerOnceToContainerBuilder;

        public WireTapper(ContainerBuilder containerBuilder)
        {
            if (containerBuilder == null)
            {
                throw new ArgumentNullException(nameof(containerBuilder));
            }

            _wireTapFactoryRegistrations = new Dictionary<Type, List<WireTapFactoryRegistration>>();
            _containerBuilder = containerBuilder;
        }


        /// <summary>
        /// The default setting used for whether or not to use single instance 
        /// behavior when wire tapping types.
        /// </summary>
        public const WireTapSingleInstance DefaultSingleInstance = DEFAULT_SINGLE_INSTANCE;

        /// <summary>
        /// The default setting used for whether an exception should be thrown 
        /// when a wire tap has been registered for a type that is not 
        /// registered at the time a <see cref="IContainer"/> is built from 
        /// the <see cref="ContainerBuilder"/>.
        /// </summary>
        public const bool DefaultThrowOnNoRegisteredTypeToWireTap = DEFAULT_THROW_ON_NO_REGISTERED_TYPE_TO_WIRE_TAP;

        /// <summary>
        /// Provides wire tapping functionality that allows the specified type 
        /// <typeparamref name="T"/> to be intercepted and the result of
        /// <paramref name="wireTapCallback"/> is returned in its place.
        /// </summary>
        /// <typeparam name="T">
        /// The type to wire tap.
        /// </typeparam>
        /// <param name="wireTapCallback">
        /// The callback that will be used to provide an instance of type 
        /// <typeparamref name="T"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="wireTapCallback"/> is null.
        /// </exception>
        public void WireTap<T>(Func<T, object> wireTapCallback)
        {
            if (wireTapCallback == null)
            {
                throw new ArgumentNullException(nameof(wireTapCallback));
            }

            WireTap<T>(
                wireTapCallback,
                DEFAULT_SINGLE_INSTANCE,
                DEFAULT_THROW_ON_NO_REGISTERED_TYPE_TO_WIRE_TAP);
        }

        /// <summary>
        /// Provides wire tapping functionality that allows the specified type 
        /// <typeparamref name="T"/> to be intercepted and the result of
        /// <paramref name="wireTapCallback"/> is returned in its place.
        /// </summary>
        /// <typeparam name="T">
        /// The type to wire tap.
        /// </typeparam>
        /// <param name="wireTapCallback">
        /// The callback that will be used to provide an instance of type 
        /// <typeparamref name="T"/>.
        /// </param>
        /// <param name="throwOnNoRegisteredTypeToWireTap">
        /// Whether or not to throw an exception when a wire tap has been 
        /// registered for a type that is not registered at the time a 
        /// <see cref="IContainer"/> is built from the 
        /// <see cref="ContainerBuilder"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="wireTapCallback"/> is null.
        /// </exception>
        public void WireTap<T>(
            Func<T, object> wireTapCallback,
            bool throwOnNoRegisteredTypeToWireTap)
        {
            if (wireTapCallback == null)
            {
                throw new ArgumentNullException(nameof(wireTapCallback));
            }

            WireTap<T>(
                wireTapCallback,
                DEFAULT_SINGLE_INSTANCE,
                throwOnNoRegisteredTypeToWireTap);
        }

        /// <summary>
        /// Provides wire tapping functionality that allows the specified type 
        /// <typeparamref name="T"/> to be intercepted and the result of
        /// <paramref name="wireTapCallback"/> is returned in its place.
        /// </summary>
        /// <typeparam name="T">
        /// The type to wire tap.
        /// </typeparam>
        /// <param name="wireTapCallback">
        /// The callback that will be used to provide an instance of type 
        /// <typeparamref name="T"/>.
        /// </param>
        /// <param name="singleInstance">
        /// Whether or not to use single instance behavior when wire tapping 
        /// types.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="wireTapCallback"/> is null.
        /// </exception>
        public void WireTap<T>(
            Func<T, object> wireTapCallback,
            WireTapSingleInstance singleInstance)
        {
            if (wireTapCallback == null)
            {
                throw new ArgumentNullException(nameof(wireTapCallback));
            }

            WireTap<T>(
                wireTapCallback,
                singleInstance,
                DEFAULT_THROW_ON_NO_REGISTERED_TYPE_TO_WIRE_TAP);
        }

        /// <summary>
        /// Provides wire tapping functionality that allows the specified type 
        /// <typeparamref name="T"/> to be intercepted and the result of
        /// <paramref name="wireTapCallback"/> is returned in its place.
        /// </summary>
        /// <typeparam name="T">
        /// The type to wire tap.
        /// </typeparam>
        /// <param name="wireTapCallback">
        /// The callback that will be used to provide an instance of type 
        /// <typeparamref name="T"/>.
        /// </param>
        /// <param name="singleInstance">
        /// Whether or not to use single instance behavior when wire tapping 
        /// types.
        /// </param>
        /// <param name="throwOnNoRegisteredTypeToWireTap">
        /// Whether or not to throw an exception when a wire tap has been 
        /// registered for a type that is not registered at the time a 
        /// <see cref="IContainer"/> is built from the 
        /// <see cref="ContainerBuilder"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="wireTapCallback"/> is null.
        /// </exception>
        public void WireTap<T>(
            Func<T, object> wireTapCallback,
            WireTapSingleInstance singleInstance,
            bool throwOnNoRegisteredTypeToWireTap)
        {
            if (wireTapCallback == null)
            {
                throw new ArgumentNullException(nameof(wireTapCallback));
            }

            WireTap<T>(
                (c, x) => wireTapCallback.Invoke(x),
                singleInstance,
                throwOnNoRegisteredTypeToWireTap);
        }

        /// <summary>
        /// Provides wire tapping functionality that allows the specified type 
        /// <typeparamref name="T"/> to be intercepted and the result of
        /// <paramref name="wireTapCallback"/> is returned in its place.
        /// </summary>
        /// <typeparam name="T">
        /// The type to wire tap.
        /// </typeparam>
        /// <param name="wireTapCallback">
        /// The callback that will be used to provide an instance of type 
        /// <typeparamref name="T"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="wireTapCallback"/> is null.
        /// </exception>
        public void WireTap<T>(
            Func<IComponentContext, T, object> wireTapCallback)
        {
            if (wireTapCallback == null)
            {
                throw new ArgumentNullException(nameof(wireTapCallback));
            }

            WireTap<T>(
                wireTapCallback,
                DEFAULT_SINGLE_INSTANCE,
                DEFAULT_THROW_ON_NO_REGISTERED_TYPE_TO_WIRE_TAP);
        }

        /// <summary>
        /// Provides wire tapping functionality that allows the specified type 
        /// <typeparamref name="T"/> to be intercepted and the result of
        /// <paramref name="wireTapCallback"/> is returned in its place.
        /// </summary>
        /// <typeparam name="T">
        /// The type to wire tap.
        /// </typeparam>
        /// <param name="wireTapCallback">
        /// The callback that will be used to provide an instance of type 
        /// <typeparamref name="T"/>.
        /// </param>
        /// <param name="singleInstance">
        /// Whether or not to use single instance behavior when wire tapping 
        /// types.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="wireTapCallback"/> is null.
        /// </exception>
        public void WireTap<T>(
            Func<IComponentContext, T, object> wireTapCallback,
            WireTapSingleInstance singleInstance)
        {
            if (wireTapCallback == null)
            {
                throw new ArgumentNullException(nameof(wireTapCallback));
            }

            WireTap<T>(
                wireTapCallback,
                singleInstance,
                DEFAULT_THROW_ON_NO_REGISTERED_TYPE_TO_WIRE_TAP);
        }

        /// <summary>
        /// Provides wire tapping functionality that allows the specified type 
        /// <typeparamref name="T"/> to be intercepted and the result of
        /// <paramref name="wireTapCallback"/> is returned in its place.
        /// </summary>
        /// <typeparam name="T">
        /// The type to wire tap.
        /// </typeparam>
        /// <param name="wireTapCallback">
        /// The callback that will be used to provide an instance of type 
        /// <typeparamref name="T"/>.
        /// </param>
        /// <param name="throwOnNoRegisteredTypeToWireTap">
        /// Whether or not to throw an exception when a wire tap has been 
        /// registered for a type that is not registered at the time a 
        /// <see cref="IContainer"/> is built from the 
        /// <see cref="ContainerBuilder"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="wireTapCallback"/> is null.
        /// </exception>
        public void WireTap<T>(
            Func<IComponentContext, T, object> wireTapCallback,
            bool throwOnNoRegisteredTypeToWireTap)
        {
            if (wireTapCallback == null)
            {
                throw new ArgumentNullException(nameof(wireTapCallback));
            }

            WireTap<T>(
                wireTapCallback,
                DEFAULT_SINGLE_INSTANCE,
                throwOnNoRegisteredTypeToWireTap);
        }

        /// <summary>
        /// Provides wire tapping functionality that allows the specified type 
        /// <typeparamref name="T"/> to be intercepted and the result of
        /// <paramref name="wireTapCallback"/> is returned in its place.
        /// </summary>
        /// <typeparam name="T">
        /// The type to wire tap.
        /// </typeparam>
        /// <param name="wireTapCallback">
        /// The callback that will be used to provide an instance of type 
        /// <typeparamref name="T"/>.
        /// </param>
        /// <param name="singleInstance">
        /// Whether or not to use single instance behavior when wire tapping 
        /// types.
        /// </param>
        /// <param name="throwOnNoRegisteredTypeToWireTap">
        /// Whether or not to throw an exception when a wire tap has been 
        /// registered for a type that is not registered at the time a 
        /// <see cref="IContainer"/> is built from the 
        /// <see cref="ContainerBuilder"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="wireTapCallback"/> is null.
        /// </exception>
        public void WireTap<T>(
            Func<IComponentContext, T, object> wireTapCallback,
            WireTapSingleInstance singleInstance,
            bool throwOnNoRegisteredTypeToWireTap)
        {
            if (wireTapCallback == null)
            {
                throw new ArgumentNullException(nameof(wireTapCallback));
            }

            WireTap(
                typeof(T),
                (c, x) => wireTapCallback(c, (T)x),
                singleInstance,
                throwOnNoRegisteredTypeToWireTap);
        }

        /// <summary>
        /// Provides wire tapping functionality that allows the specified type 
        /// <paramref name="typeToWireTap"/> to be intercepted and the result of
        /// <paramref name="wireTapCallback"/> is returned in its place.
        /// </summary>
        /// <param name="typeToWireTap">
        /// The type to wire tap.
        /// </param>
        /// <param name="wireTapCallback">
        /// The callback that will be used to provide an instance of type 
        /// <paramref name="typeToWireTap"/>.
        /// </param>
        /// <param name="singleInstance">
        /// Whether or not to use single instance behavior when wire tapping 
        /// types.
        /// </param>
        /// <param name="throwOnNoRegisteredTypeToWireTap">
        /// Whether or not to throw an exception when a wire tap has been 
        /// registered for a type that is not registered at the time a 
        /// <see cref="IContainer"/> is built from the 
        /// <see cref="ContainerBuilder"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="typeToWireTap"/> or <paramref name="wireTapCallback"/> are null.
        /// </exception>
        public void WireTap(
            Type typeToWireTap,
            Func<IComponentContext, object, object> wireTapCallback,
            WireTapSingleInstance singleInstance = WireTapSingleInstance.Inherit,
            bool throwOnNoRegisteredTypeToWireTap = true)
        {
            if (typeToWireTap == null)
            {
                throw new ArgumentNullException(nameof(typeToWireTap));
            }

            if (wireTapCallback == null)
            {
                throw new ArgumentNullException(nameof(wireTapCallback));
            }

            var wireTapFactoryRegistrationsForType = GetWireTapFactoryCollectionForType(typeToWireTap);

            var factoryRegistration = new WireTapFactoryRegistration(
                typeToWireTap,
                (c, x) =>
                {
                    var wireTapResult = wireTapCallback(c, x);
                    if (wireTapResult == null)
                    {
                        throw new InvalidOperationException(
                            $"Cannot return null from a wire tap for type " +
                            $"'{typeToWireTap.FullName}' because Autofac does not " +
                            $"allow null registrations.");
                    }

                    if (!typeToWireTap.IsAssignableFrom(wireTapResult.GetType()))
                    {
                        throw new InvalidOperationException(
                            $"The wire tap result is not assignable to the type " +
                            $"that is being wire tapped. The result of the wire " +
                            $"tap was of type '{wireTapResult.GetType().FullName}' " +
                            $"but the type being wire tapped is of type " +
                            $"'{typeToWireTap.FullName}'.");
                    }

                    return wireTapResult;
                },
                throwOnNoRegisteredTypeToWireTap,
                singleInstance);
            wireTapFactoryRegistrationsForType.Add(factoryRegistration);

            if (_registerOnceToContainerBuilder)
            {
                return;
            }

            _registerOnceToContainerBuilder = true;
            RegisterContainerBuilderWireTappingEvents();
        }

        private void RegisterContainerBuilderWireTappingEvents()
        {
            _containerBuilder.ComponentRegistryBuilder.Registered += (_, componentRegisteredEventArgs) => ContainerBuilder_ComponentRegistryBuilderRegistered(componentRegisteredEventArgs);

            _containerBuilder.RegisterBuildCallback(scope =>
            {
                foreach (var entry in _wireTapFactoryRegistrations)
                {
                    if (!entry.Value.Any(x => x.ThrowOnNoRegisteredTypeToWireTap))
                    {
                        continue;
                    }

                    if (!scope.IsRegistered(entry.Key))
                    {
                        throw new InvalidOperationException(
                            $"Could not create a wire tap for type '{entry.Key.FullName}' " +
                            $"because there is no typed service for the specified type.");
                    }
                }
            });
        }

        private List<WireTapFactoryRegistration> GetWireTapFactoryCollectionForType(
            Type typeToWireTap)
        {
            if (!_wireTapFactoryRegistrations.TryGetValue(typeToWireTap, out var wireTapFactoriesForType))
            {
                wireTapFactoriesForType = new List<WireTapFactoryRegistration>();
                _wireTapFactoryRegistrations[typeToWireTap] = wireTapFactoriesForType;
            }

            return wireTapFactoriesForType;
        }

        private void ContainerBuilder_ComponentRegistryBuilderRegistered(ComponentRegisteredEventArgs componentRegisteredEventArgs)
        {
            // we only want to hook up to the pipeline building event for
            // types that we're trying to wire tap. this current handler will
            // trigger for many other types, so we only care about the ones
            // that are of interest for tapping.
            var matchedServiceTypes = new List<Type>();
            foreach (var service in componentRegisteredEventArgs
                .ComponentRegistration
                .Services
                .Where(x => x is TypedService)
                .Cast<TypedService>())
            {
                if (_wireTapFactoryRegistrations.TryGetValue(
                    service.ServiceType,
                    out var matchingRegistrations))
                {
                    componentRegisteredEventArgs.ComponentRegistration.PipelineBuilding += (__, resolvePipelineBuilder) => ComponentRegistry_PipelineBuilding(
                        resolvePipelineBuilder,
                        matchingRegistrations);
                    matchedServiceTypes.Add(service.ServiceType);
                }
            }

            if (matchedServiceTypes.Count > 1 &&
                componentRegisteredEventArgs.ComponentRegistration.Sharing == InstanceSharing.Shared)
            {
                throw new InvalidOperationException(
                    $"A (currently) unsupported registration+wire " +
                    $"tapping combination has been detected. The type " +
                    $"to wire tap '{componentRegisteredEventArgs.ComponentRegistration.Activator.LimitType.FullName} " +
                    $"has been registered as SingleInstance and there " +
                    $"are unique wire taps registered for the " +
                    $"following 'services' associated with it: " +
                    $"{string.Join(", ", matchedServiceTypes.Select(st => $"'{st.FullName}'"))}." +
                    $"\r\n" +
                    $"It does not (yet) seem possible to override the " +
                    $"behavior to get the cached single instance that " +
                    $"Autofac will resolve, so the following wire taps " +
                    $"will never run as they are expected to.");
            }
        }

        private void ComponentRegistry_PipelineBuilding(
            IResolvePipelineBuilder resolvePipelineBuilder,
            IReadOnlyCollection<WireTapFactoryRegistration> factoryRegistrations)
        {
            resolvePipelineBuilder.Use(PipelinePhase.Activation, MiddlewareInsertionMode.EndOfPhase, (c, next) =>
            {
                // need to call this to keep the pipeline flowing
                next(c);

                if (!(c.Service is TypedService typedService))
                {
                    return;
                }

                var newInstance = c.Instance;

                // we go through our factory registrations in order like their
                // own little pipeline
                foreach (var registration in factoryRegistrations)
                {
                    // skip over incompatible wire-taps, which can get
                    // registered when we're dealing with multiple types being
                    // mapped to a single one (i.e. a class with multiple
                    // interfaces that it's registered for)
                    if (!typedService.ServiceType.IsAssignableFrom(registration.TypeToWireTap))
                    {
                        continue;
                    }

                    // map state properly if we need to inherit the sharing (or not)
                    var singleInstance = registration.SingleInstance;
                    if (singleInstance == WireTapSingleInstance.Inherit)
                    {
                        singleInstance = c.Registration.Sharing == InstanceSharing.None
                            ? WireTapSingleInstance.NewInstances
                            : WireTapSingleInstance.SingleInstance;
                    }

                    // sanity check to avoid odd behavior
                    if (c.Registration.Sharing == InstanceSharing.Shared &&
                        singleInstance != WireTapSingleInstance.SingleInstance)
                    {
                        throw new ArgumentException(
                            $"The type to be wire-tapped is single " +
                            $"instance and after the first resolution " +
                            $"results in a wire-tap, the subsequent " +
                            $"resolutions will use the existing " +
                            $"instance. If you want new wire-tap " +
                            $"instances, it is suggested that you " +
                            $"change the registration of the type to " +
                            $"be wire-tapped to not be single instance.");
                    }

                    // get a cached instance if we expect to and we have one
                    if (singleInstance == WireTapSingleInstance.SingleInstance &&
                        registration.InstanceCache != null)
                    {
                        newInstance = registration.InstanceCache;
                        continue;
                    }

                    // actually perform the wire tapping                
                    newInstance = registration.Factory(
                        c,
                        newInstance);

                    // cache if we need to
                    if (singleInstance == WireTapSingleInstance.SingleInstance)
                    {
                        registration.InstanceCache = newInstance;
                    }
                }

                // mark the new instance to actually assign the wire-tapped service
                c.Instance = newInstance;
            });
        }

        private class WireTapFactoryRegistration
        {
            public WireTapFactoryRegistration(
                Type typeToWireTap,
                WireTapFactory factory,
                bool throwOnNoRegisteredTypeToWireTap,
                WireTapSingleInstance singleInstance)
            {
                TypeToWireTap = typeToWireTap;
                Factory = factory;
                ThrowOnNoRegisteredTypeToWireTap = throwOnNoRegisteredTypeToWireTap;
                SingleInstance = singleInstance;
            }

            public Type TypeToWireTap { get; }

            public WireTapFactory Factory { get; }

            public bool ThrowOnNoRegisteredTypeToWireTap { get; }

            public WireTapSingleInstance SingleInstance { get; }

            public object InstanceCache { get; set; }
        }
    }
}
