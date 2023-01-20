using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;

using Autofac.Builder;

namespace Autofac
{
    public delegate void TypeRegisterDelegate(IRegistrationBuilder<object, ConcreteReflectionActivatorData, SingleRegistrationStyle> registrationBuilder);

    public static class TypeRegistration
    {
        public static void RegisterAssemblyTypes(
            this ContainerBuilder containerBuilder,
            Assembly assembly,
            Predicate<Type> filter,
            TypeRegisterDelegate typeRegisterCallback) =>
            RegisterTypes(
                containerBuilder,
                assembly.GetTypes(),
                filter,
                typeRegisterCallback);

        public static void RegisterAssemblyTypes<T>(
            this ContainerBuilder containerBuilder,
            Assembly assembly,
            TypeRegisterDelegate typeRegisterCallback)
        {
            containerBuilder.RegisterAssemblyTypes(
                assembly,
                t => typeof(T).IsAssignableFrom(t) && t.IsClass && !t.IsAbstract,
                typeRegisterCallback);
        }

        public static void RegisterAssemblyTypesAsSingletonInterfaces<T>(
            this ContainerBuilder containerBuilder,
            Assembly assembly)
        {
            containerBuilder.RegisterAssemblyTypes<T>(
                assembly,
                x => x.SingleInstance().AsImplementedInterfaces());
        }

        public static void RegisterTypes(
            this ContainerBuilder containerBuilder,
            IEnumerable<Type> types,
            Predicate<Type> filter,
            TypeRegisterDelegate typeRegisterCallback)
        {
            foreach (var type in types.Where(x => filter(x)))
            {
                typeRegisterCallback.Invoke(containerBuilder.RegisterType(type));
            }
        }

        /// <summary>
        /// An extension method that will iterate through <see cref="Type"/>s 
        /// in the specified <see cref="Assembly"/> that have a 
        /// <see cref="DiscoverableForRegistrationAttribute"/>.
        /// </summary>
        /// <param name="containerBuilder">
        /// The <see cref="ContainerBuilder"/> instance.
        /// </param>
        /// <param name="assembly">
        /// The <see cref="Assembly"/> to check for <see cref="Type"/>s.
        /// </param>
        /// <remarks>
        /// It is likely unwise to call this method from library code (instead 
        /// of an entry point) unless you can guarantee it runs once. 
        /// Otherwise you may be calling registrations far more times than 
        /// necessary.
        /// </remarks>
        public static void RegisterDiscoverableTypes(
            this ContainerBuilder containerBuilder,
            Assembly assembly)
        {
            foreach (var entry in assembly
                .GetTypes()
                .Select(x => new
                {
                    Attribute = x.GetCustomAttribute<DiscoverableForRegistrationAttribute>(true),
                    Type = x,
                })
                .Where(x => x.Attribute != null))
            {
                var registrationBuilder = containerBuilder.RegisterType(entry.Type);
                if (entry.Attribute.SingleInstance)
                {
                    registrationBuilder = registrationBuilder.SingleInstance();
                }

                if (entry.Attribute.DiscoveryRegistrationMode == DiscoveryRegistrationMode.Self ||
                    entry.Attribute.DiscoveryRegistrationMode == DiscoveryRegistrationMode.SelfAndImplementedInterfaces)
                {
                    registrationBuilder = registrationBuilder.Keyed(typeof(DiscoverableForRegistrationAttribute), entry.Type);
                }

                if (entry.Attribute.DiscoveryRegistrationMode == DiscoveryRegistrationMode.ImplementedInterfaces ||
                    entry.Attribute.DiscoveryRegistrationMode == DiscoveryRegistrationMode.SelfAndImplementedInterfaces)
                {
                    foreach (var inheritedInterface in entry.Type.GetInterfaces())
                    {
                        registrationBuilder = registrationBuilder.Keyed(typeof(DiscoverableForRegistrationAttribute), inheritedInterface);
                    }
                }
            }
        }
    }
}
