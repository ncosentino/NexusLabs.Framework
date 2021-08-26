using System;
using System.Collections.Generic;
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

        public static IEnumerable<PropertyInfo> GetPublicProperties(this Type type)
        {
            if (type.IsInterface)
            {
                var considered = new List<Type>();
                var queue = new Queue<Type>();
                considered.Add(type);
                queue.Enqueue(type);

                while (queue.Count > 0)
                {
                    var subType = queue.Dequeue();
                    foreach (var subInterface in subType.GetInterfaces())
                    {
                        if (considered.Contains(subInterface))
                        {
                            continue;
                        }

                        considered.Add(subInterface);
                        queue.Enqueue(subInterface);
                    }

                    var typeMembers = subType.GetProperties(
                        BindingFlags.FlattenHierarchy |
                        BindingFlags.Public | 
                        BindingFlags.Instance);

                    foreach (var member in typeMembers)
                    {
                        yield return member;
                    }
                }
            }

            foreach (var member in type.GetProperties(
                BindingFlags.FlattenHierarchy |
                BindingFlags.Public | 
                BindingFlags.Instance)
            )
            {
                yield return member;
            }
        }

        public static IEnumerable<MethodInfo> GetPublicMethods(this Type type)
        {
            if (type.IsInterface)
            {
                var considered = new List<Type>();
                var queue = new Queue<Type>();
                considered.Add(type);
                queue.Enqueue(type);

                while (queue.Count > 0)
                {
                    var subType = queue.Dequeue();
                    foreach (var subInterface in subType.GetInterfaces())
                    {
                        if (considered.Contains(subInterface))
                        {
                            continue;
                        }

                        considered.Add(subInterface);
                        queue.Enqueue(subInterface);
                    }

                    var typeMembers = subType.GetMethods(
                        BindingFlags.FlattenHierarchy |
                        BindingFlags.Public |
                        BindingFlags.Instance);

                    foreach (var member in typeMembers)
                    {
                        yield return member;
                    }
                }
            }

            foreach (var member in type.GetMethods(
                BindingFlags.FlattenHierarchy |
                BindingFlags.Public |
                BindingFlags.Instance)
            )
            {
                yield return member;
            }
        }
    }
}