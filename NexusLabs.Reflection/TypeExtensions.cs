using System;
using System.Linq;
using System.Reflection;

using NexusLabs.Contracts;

namespace NexusLabs.Reflection
{
    public static class TypeExtensions
    {
        public static object CreateInstance(
            this Type type,
            Predicate<ConstructorInfo> constructorFilter,
            object[] parameters = null)
        {
            ArgumentContract.RequiresNotNull(type, nameof(type));
            ArgumentContract.RequiresNotNull(constructorFilter, nameof(constructorFilter));

            var constructors = type.GetConstructors(
                BindingFlags.NonPublic |
                BindingFlags.Public |
                BindingFlags.Instance);
            var constructor = constructors.FirstOrDefault(c => constructorFilter.Invoke(c));
            if (constructor == null)
            {
                throw new InvalidOperationException(
                    $"No valid constructor available to create instance of '{type}'.");
            }

            var instance = constructor.Invoke(parameters ?? new object[]
            {
            });
            return instance;
        }

        public static T CreateInstance<T>(
            this Type type,
            Predicate<ConstructorInfo> constructorFilter,
            object[] parameters = null)
        {
            var instance = type.CreateInstance(
                constructorFilter,
                parameters);
            return (T)instance;
        }
    }
}