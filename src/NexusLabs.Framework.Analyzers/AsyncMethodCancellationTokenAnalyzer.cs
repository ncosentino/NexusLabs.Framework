using System;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace NexusLabs.Framework.Analyzers;

/// <summary>
/// Flags methods that are async — carrying the <c>async</c> keyword OR whose
/// name ends with the <c>Async</c> suffix — but declare no
/// <c>System.Threading.CancellationToken</c> parameter. The rule enforces only
/// the PRESENCE of the token; CA1068 enforces its last-position. Overrides,
/// interface implementations, <c>async void</c> event handlers
/// (<c>(object, EventArgs)</c>), test methods, members named by a test data
/// source attribute such as <c>[MemberData]</c>, <c>Main</c>, sibling overloads
/// (a same-named method in the same type takes a token), and methods accepting
/// delegate parameters are exempt.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AsyncMethodCancellationTokenAnalyzer : DiagnosticAnalyzer
{
    private const string AsyncSuffix = "Async";

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.AsyncMethodMustDeclareCancellationToken);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(static compilationStartContext =>
        {
            var dataSourceMembers = new TestDataSourceMemberIndex(compilationStartContext.Compilation);

            compilationStartContext.RegisterSyntaxNodeAction(
                nodeContext => AnalyzeMethod(nodeContext, dataSourceMembers),
                SyntaxKind.MethodDeclaration);
        });
    }

    private static void AnalyzeMethod(
        SyntaxNodeAnalysisContext context,
        TestDataSourceMemberIndex dataSourceMembers)
    {
        var methodDeclaration = (MethodDeclarationSyntax)context.Node;

        var hasAsyncKeyword = methodDeclaration.Modifiers.Any(SyntaxKind.AsyncKeyword);
        var hasAsyncSuffix = HasAsyncSuffix(methodDeclaration.Identifier.Text);
        if (!hasAsyncKeyword && !hasAsyncSuffix)
        {
            return;
        }

        var method = context.SemanticModel.GetDeclaredSymbol(
            methodDeclaration,
            context.CancellationToken);
        if (method is null)
        {
            return;
        }

        if (method.IsOverride
            || IsInterfaceImplementation(method)
            || IsTestMethod(method)
            || IsEventHandler(method)
            || (method.IsStatic && method.Name == "Main")
            || HasCancellationTokenParameter(method)
            || HasSiblingOverloadWithCancellationToken(method)
            || HasDelegateParameter(method)
            || dataSourceMembers.Contains(method, context.CancellationToken))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.AsyncMethodMustDeclareCancellationToken,
            methodDeclaration.Identifier.GetLocation(),
            method.Name));
    }

    private static bool HasAsyncSuffix(string name)
    {
        return name.Length > AsyncSuffix.Length
            && name.EndsWith(AsyncSuffix, StringComparison.Ordinal);
    }

    private static bool HasCancellationTokenParameter(IMethodSymbol method)
    {
        return method.Parameters.Any(static p => IsCancellationToken(p.Type));
    }

    private static bool IsCancellationToken(ITypeSymbol type)
    {
        return type.Name == "CancellationToken"
            && type.ContainingNamespace?.ToDisplayString() == "System.Threading";
    }

    private static bool IsTestMethod(IMethodSymbol method)
    {
        foreach (var attribute in method.GetAttributes())
        {
            var name = attribute.AttributeClass?.Name;
            if (name is null)
            {
                continue;
            }

            if (name.EndsWith("Attribute", StringComparison.Ordinal))
            {
                name = name.Substring(0, name.Length - "Attribute".Length);
            }

            if (name is "Fact" or "Theory" or "Test" or "TestCase" or "TestMethod" or "DataTestMethod")
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsEventHandler(IMethodSymbol method)
    {
        if (!method.ReturnsVoid || method.Parameters.Length != 2)
        {
            return false;
        }

        if (method.Parameters[0].Type.SpecialType != SpecialType.System_Object)
        {
            return false;
        }

        return InheritsFromEventArgs(method.Parameters[1].Type);
    }

    private static bool InheritsFromEventArgs(ITypeSymbol type)
    {
        for (ITypeSymbol? current = type; current is not null; current = current.BaseType)
        {
            if (current.Name == "EventArgs"
                && current.ContainingNamespace?.ToDisplayString() == "System")
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsInterfaceImplementation(IMethodSymbol method)
    {
        if (method.ExplicitInterfaceImplementations.Length > 0)
        {
            return true;
        }

        var containingType = method.ContainingType;
        if (containingType is null)
        {
            return false;
        }

        foreach (var interfaceType in containingType.AllInterfaces)
        {
            foreach (var member in interfaceType.GetMembers(method.Name).OfType<IMethodSymbol>())
            {
                var implementation = containingType.FindImplementationForInterfaceMember(member);
                if (SymbolEqualityComparer.Default.Equals(implementation, method))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool HasSiblingOverloadWithCancellationToken(IMethodSymbol method)
    {
        var containingType = method.ContainingType;
        if (containingType is null)
        {
            return false;
        }

        foreach (var member in containingType.GetMembers(method.Name).OfType<IMethodSymbol>())
        {
            if (SymbolEqualityComparer.Default.Equals(member, method))
            {
                continue;
            }

            if (HasCancellationTokenParameter(member))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasDelegateParameter(IMethodSymbol method)
    {
        foreach (var parameter in method.Parameters)
        {
            if (parameter.Type.TypeKind == TypeKind.Delegate)
            {
                return true;
            }

            if (IsDelegateBaseType(parameter.Type))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsDelegateBaseType(ITypeSymbol type)
    {
        var displayString = type.ToDisplayString();
        return displayString == "System.Delegate" || displayString == "System.MulticastDelegate";
    }
}
