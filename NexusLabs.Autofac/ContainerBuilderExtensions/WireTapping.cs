using System;
using System.Collections.Generic;

using Autofac.Core;

namespace Autofac
{
    public static class WireTapping
    {
        private delegate object WireTapFactory(
            IComponentContext componentContext,
            object instance);

        private static Dictionary<ContainerBuilder, Dictionary<Type, object>> _singleInstanceCache =
            new Dictionary<ContainerBuilder, Dictionary<Type, object>>();
        private static Dictionary<ContainerBuilder, Dictionary<Type, List<WireTapFactory>>> _wireTapFactories =
            new Dictionary<ContainerBuilder, Dictionary<Type, List<WireTapFactory>>>();

        public static void WireTap<T>(
            this ContainerBuilder containerBuilder,
            Func<T, object> wireTapCallback)
        {
            containerBuilder.WireTap<T>(wireTapCallback, WireTapSingleInstance.Inherit);
        }

        public static void WireTap<T>(
            this ContainerBuilder containerBuilder,
            Func<T, object> wireTapCallback,
            WireTapSingleInstance singleInstance)
        {
            containerBuilder.WireTap<T>((c, x) => wireTapCallback.Invoke(x), singleInstance);
        }

        public static void WireTap<T>(
            this ContainerBuilder containerBuilder,
            Func<IComponentContext, T, object> wireTapCallback)
        {
            containerBuilder.WireTap<T>(wireTapCallback, WireTapSingleInstance.Inherit);
        }

        public static void WireTap<T>(
            this ContainerBuilder containerBuilder,
            Func<IComponentContext, T, object> wireTapCallback,
            WireTapSingleInstance singleInstance)
        {
            containerBuilder
                .Register(componentContext =>
                {
                    if (!componentContext
                        .ComponentRegistry
                        .TryGetRegistration(
                            new TypedService(typeof(T)),
                            out var componentRegistration))
                    {
                        throw new InvalidOperationException(
                            $"Could not create a wire tap for type '{typeof(T)}' " +
                            $"because there is no typed service for the specified type.");
                    }

                    if (!_wireTapFactories.TryGetValue(containerBuilder, out var wireTapFactoriesForContainerBuilder))
                    {
                        wireTapFactoriesForContainerBuilder = new Dictionary<Type, List<WireTapFactory>>();
                        _wireTapFactories[containerBuilder] = wireTapFactoriesForContainerBuilder;
                    }

                    if (!wireTapFactoriesForContainerBuilder.TryGetValue(typeof(T), out var wireTapFactoriesForType))
                    {
                        void handler(object s, ActivatingEventArgs<object> e)
                        {
                            var activatedRegistration = (IComponentRegistration)s;

                            if (singleInstance == WireTapSingleInstance.Inherit)
                            {
                                singleInstance = activatedRegistration.Sharing == InstanceSharing.None
                                    ? WireTapSingleInstance.NewInstances
                                    : WireTapSingleInstance.SingleInstance;
                            }

                            if (activatedRegistration.Sharing == InstanceSharing.Shared &&
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

                            if (singleInstance == WireTapSingleInstance.SingleInstance &&
                                _singleInstanceCache.TryGetValue(containerBuilder, out var instanceCache) &&
                                instanceCache.TryGetValue(typeof(T), out var cachedInstance))
                            {
                                e.Instance = cachedInstance;
                                return;
                            }

                            var factories = wireTapFactoriesForContainerBuilder[typeof(T)];
                            var newInstance = (T)e.Instance;
                            foreach (var factory in factories)
                            {
                                newInstance = (T)factory(
                                    e.Context,
                                    newInstance);
                            }

                            e.Instance = newInstance;

                            if (singleInstance == WireTapSingleInstance.SingleInstance)
                            {
                                if (!_singleInstanceCache.TryGetValue(containerBuilder, out instanceCache))
                                {
                                    instanceCache = new Dictionary<Type, object>();
                                    _singleInstanceCache[containerBuilder] = instanceCache;
                                }

                                instanceCache[typeof(T)] = newInstance;
                            }
                        };

                        componentRegistration.Activating += handler;

                        wireTapFactoriesForType = new List<WireTapFactory>();
                        wireTapFactoriesForContainerBuilder[typeof(T)] = wireTapFactoriesForType;
                    }

                    wireTapFactoriesForType.Insert(0, (c, x) => wireTapCallback(c, (T)x));

                    return new Ignore();
                })
                .AutoActivate();
        }

        private class Ignore
        {
        }
    }
}
