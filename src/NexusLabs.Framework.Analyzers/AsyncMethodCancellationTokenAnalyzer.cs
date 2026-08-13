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
/// the PRESENCE of the token; CA1068 enforces its last-position.
/// </summary>
/// <remarks>
/// Every exemption follows one principle: the rule never demands a parameter
/// the author is not free to add. Exempt are overrides, interface
/// implementations, <c>async void</c> event handlers
/// (<c>(object, EventArgs)</c>), test and benchmark callbacks, members named by
/// a test data source attribute such as <c>[MemberData]</c>, ASP.NET Core
/// middleware entry points, public SignalR hub methods, methods converted to a
/// delegate, <c>Main</c>, sibling overloads (a same-named method in the same
/// type takes a token), and methods accepting delegate parameters.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AsyncMethodCancellationTokenAnalyzer : DiagnosticAnalyzer
{
    private const string AsyncSuffix = "Async";

    /// <summary>
    /// Attributes whose presence means a test or benchmark framework owns the
    /// method's signature and invokes it directly, so there is no caller that
    /// could thread a token.
    /// </summary>
    private static readonly ImmutableHashSet<string> FrameworkInvokedAttributeNames =
        ImmutableHashSet.Create(
            StringComparer.Ordinal,
            "Fact",
            "Theory",
            "Test",
            "TestCase",
            "TestMethod",
            "DataTestMethod",
            "Benchmark",
            "GlobalSetup",
            "GlobalCleanup",
            "IterationSetup",
            "IterationCleanup");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.AsyncMethodMustDeclareCancellationToken);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(static compilationStartContext =>
        {
            var dataSourceMembers = new TestDataSourceMemberIndex(compilationStartContext.Compilation);
            var delegateTargets = new DelegateTargetMemberIndex(compilationStartContext.Compilation);

            compilationStartContext.RegisterSyntaxNodeAction(
                nodeContext => AnalyzeMethod(nodeContext, dataSourceMembers, delegateTargets),
                SyntaxKind.MethodDeclaration);
        });
    }

    private static void AnalyzeMethod(
        SyntaxNodeAnalysisContext context,
        TestDataSourceMemberIndex dataSourceMembers,
        DelegateTargetMemberIndex delegateTargets)
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
            || IsFrameworkInvokedMethod(method)
            || IsMiddlewareEntryPoint(method)
            || IsSignalRHubMethod(method)
            || IsEventHandler(method)
            || (method.IsStatic && method.Name == "Main")
            || HasCancellationTokenParameter(method)
            || HasSiblingOverloadWithCancellationToken(method)
            || HasDelegateParameter(method)
            || dataSourceMembers.Contains(method, context.CancellationToken)
            || delegateTargets.Contains(method, context.CancellationToken))
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

    /// <summary>
    /// Recognizes the conventional ASP.NET Core middleware entry point. The
    /// framework populates parameters after <c>HttpContext</c> from DI, so an
    /// added token would be resolved as a service rather than a cancellation
    /// source; request cancellation is <c>HttpContext.RequestAborted</c>.
    /// </summary>
    private static bool IsMiddlewareEntryPoint(IMethodSymbol method)
    {
        if (method.Name is not ("Invoke" or "InvokeAsync") || method.Parameters.Length == 0)
        {
            return false;
        }

        return IsNamedType(method.Parameters[0].Type, "Microsoft.AspNetCore.Http", "HttpContext");
    }

    /// <summary>
    /// Recognizes a client-callable SignalR hub method. Every public instance
    /// method on a hub is part of the wire contract, so an added parameter
    /// changes what clients must send; connection cancellation is
    /// <c>Context.ConnectionAborted</c>. Non-public helpers on a hub are not
    /// client-callable and stay covered.
    /// </summary>
    private static bool IsSignalRHubMethod(IMethodSymbol method)
    {
        if (method.IsStatic || method.DeclaredAccessibility != Accessibility.Public)
        {
            return false;
        }

        return InheritsFrom(method.ContainingType, "Microsoft.AspNetCore.SignalR", "Hub");
    }

    private static bool IsFrameworkInvokedMethod(IMethodSymbol method)
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

            if (FrameworkInvokedAttributeNames.Contains(name))
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
        return InheritsFrom(type, "System", "EventArgs");
    }

    private static bool InheritsFrom(ITypeSymbol? type, string containingNamespace, string name)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (IsNamedType(current, containingNamespace, name))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsNamedType(ITypeSymbol type, string containingNamespace, string name)
    {
        return type.Name == name
            && type.ContainingNamespace?.ToDisplayString() == containingNamespace;
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
