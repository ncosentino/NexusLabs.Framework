using System.Linq;

using Microsoft.CodeAnalysis;

namespace NexusLabs.Framework.Analyzers;

internal static class TryMethodConvention
{
    public static bool IsTryPrefixed(string methodName) =>
        methodName.Length > 3
        && methodName.StartsWith("Try", System.StringComparison.Ordinal)
        && char.IsUpper(methodName[3]);

    public static bool IsInterfaceImplementation(
        IMethodSymbol method,
        IAssemblySymbol? ownedAssembly = null)
    {
        foreach (var member in method.ExplicitInterfaceImplementations)
        {
            if (!SymbolEqualityComparer.Default.Equals(member.ContainingAssembly, ownedAssembly))
            {
                return true;
            }
        }

        var containingType = method.ContainingType;
        if (containingType is null)
        {
            return false;
        }

        foreach (var iface in containingType.AllInterfaces)
        {
            if (SymbolEqualityComparer.Default.Equals(iface.ContainingAssembly, ownedAssembly))
            {
                continue;
            }

            foreach (var member in iface.GetMembers(method.Name).OfType<IMethodSymbol>())
            {
                var impl = containingType.FindImplementationForInterfaceMember(member);
                if (SymbolEqualityComparer.Default.Equals(impl, method))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
