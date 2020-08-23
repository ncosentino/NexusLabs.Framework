using System;
using System.Reflection;

namespace NexusLabs.Reflection
{
    public static class AssemblyExtensions
    {
        public static Type GetNestedType(
            this Assembly assembly,
            string parentType,
            string nestedType,
            BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException(
                    nameof(assembly),
                    "The assembly cannot be null.");
            }

            if (parentType == null)
            {
                throw new ArgumentNullException(
                    nameof(parentType),
                    "The parent type cannot be null.");
            }

            if (nestedType == null)
            {
                throw new ArgumentNullException(
                    nameof(nestedType),
                    "The nested type cannot be null.");
            }

            var type = assembly
                .GetType(parentType)
                .GetNestedType(nestedType, bindingFlags);
            return type;
        }

        public static Type GetNestedType(
            this Assembly assembly,
            string fullNestedTypeIdentifier,
            BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException(
                    nameof(assembly),
                    "The assembly cannot be null.");
            }

            if (fullNestedTypeIdentifier == null)
            {
                throw new ArgumentNullException(
                    nameof(fullNestedTypeIdentifier),
                    "The nested type identifier cannot be null.");
            }

            var split = fullNestedTypeIdentifier.Split('+');
            if (split.Length < 2)
            {
                throw new ArgumentException(
                    $"Expecting the input string to be in the format 'ParentType+NestedType'");
            }

            var type = assembly.GetType(split[0]);

            for (var splitIndex = 1; splitIndex < split.Length; splitIndex++)
            {
                type = type.GetNestedType(split[splitIndex], bindingFlags);
            }

            return type;
        }
    }
}