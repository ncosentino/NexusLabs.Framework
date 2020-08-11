using System;
using System.Linq;
using System.Reflection;

using Autofac.Core;

namespace Autofac
{
    public static class TypeRegistration
    {
        public static void RegisterAssemblyTypes(
            this ContainerBuilder containerBuilder,
            Assembly assembly,
            Predicate<Type> filter,
            Action<Builder.IRegistrationBuilder<object, Builder.ConcreteReflectionActivatorData, Builder.SingleRegistrationStyle>> typeRegisterCallback)
        {
            foreach (var controllerType in assembly
                .GetTypes()
                .Where(x => filter(x)))
            {
                typeRegisterCallback.Invoke(containerBuilder.RegisterType(controllerType));
            }
        }

        public static void RegisterAssemblyTypes<T>(
            this ContainerBuilder containerBuilder,
            Assembly assembly,
            Action<Builder.IRegistrationBuilder<object, Builder.ConcreteReflectionActivatorData, Builder.SingleRegistrationStyle>> typeRegisterCallback)
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
    }
}
