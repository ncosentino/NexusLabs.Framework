using System;
using System.Reflection;

using NexusLabs.Contracts;

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
            ArgumentContract.RequiresNotNull(assembly, nameof(assembly));
            ArgumentContract.RequiresNotNull(parentType, nameof(parentType));
            ArgumentContract.RequiresNotNull(nestedType, nameof(nestedType));

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
            ArgumentContract.RequiresNotNull(assembly, nameof(assembly));
            ArgumentContract.RequiresNotNull(fullNestedTypeIdentifier, nameof(fullNestedTypeIdentifier));

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