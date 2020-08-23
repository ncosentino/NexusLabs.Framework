using System;
using System.Linq;
using System.Reflection;

namespace NexusLabs.Reflection
{
    public static class TypeExtensions
    {
        public static object CreateInstance(
            this Type type,
            Predicate<ConstructorInfo> constructorFilter,
            object[] parameters = null)
        {
            if (type == null)
            {
                throw new ArgumentNullException(
                    nameof(type),
                    "The type cannot be null.");
            }

            if (constructorFilter == null)
            {
                throw new ArgumentNullException(
                    nameof(constructorFilter),
                    "The constructor filter cannot be null.");
            }

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
            Predicate<ConstructorInfo> constructorFilter,
            object[] parameters = null)
        {
            var instance = CreateInstance(
                typeof(T),
                constructorFilter,
                parameters);
            return (T)instance;
        }
    }
}