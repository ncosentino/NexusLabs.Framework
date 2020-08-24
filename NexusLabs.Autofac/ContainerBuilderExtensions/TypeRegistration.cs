using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

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
            foreach (var controllerType in types.Where(x => filter(x)))
            {
                typeRegisterCallback.Invoke(containerBuilder.RegisterType(controllerType));
            }
        }
    }
}
