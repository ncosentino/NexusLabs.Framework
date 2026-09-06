using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace NexusLabs.Framework.Analyzers;

/// <summary>
/// Flags methods whose name begins with the <c>Try</c> prefix (followed by an
/// upper-case character — so <c>TryGetAsync</c>, <c>TryParse</c>, but NOT
/// <c>Trythis</c> and NOT underscore-delimited test names like
/// <c>TryAsync_Scenario_ExpectedResult</c>) but whose return type is NOT one of
/// the recognized "Try-result" shapes: <c>TriedEx&lt;T&gt;</c>,
/// <c>TriedNullEx&lt;T&gt;</c>, or <c>System.Exception?</c> (each optionally
/// wrapped in <c>Task&lt;&gt;</c> / <c>ValueTask&lt;&gt;</c>).
/// </summary>
/// <remarks>
/// The analyzer skips three categories so the rule fires at the canonical
/// declaration site only:
/// <list type="bullet">
///   <item>methods on the <c>NexusLabs.Framework.Try</c> helper class itself
///         (the convention's infrastructure)</item>
///   <item><c>override</c> members (the base owns the name)</item>
///   <item>interface implementations, both implicit and explicit (the
///         interface owns the name)</item>
/// </list>
/// It also skips names containing an underscore, because the codebase uses
/// underscore-delimited test naming (<c>MethodUnderTest_Scenario_Expectation</c>)
/// where the first segment is just the SUT name and is not itself an API
/// claiming a Try-result contract.
/// <para>
/// <c>TriedEx</c> and <c>TriedNullEx</c> are matched by type name AND the
/// <c>NexusLabs.Framework</c> namespace — types with the same names in
/// other namespaces do not satisfy the rule.
/// </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TryPrefixedMethodReturnTypeAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.TryPrefixedMethodMustReturnTryResultType);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
    }

    private static void AnalyzeMethod(SyntaxNodeAnalysisContext context)
    {
        var methodDecl = (MethodDeclarationSyntax)context.Node;
        var methodName = methodDecl.Identifier.Text;

        if (!TryMethodConvention.IsTryPrefixed(methodName) || methodName.IndexOf('_') >= 0)
        {
            return;
        }

        var methodSymbol = context.SemanticModel.GetDeclaredSymbol(
            methodDecl,
            context.CancellationToken);
        if (methodSymbol is null)
        {
            return;
        }

        if (IsOnTryHelperType(methodSymbol))
        {
            return;
        }

        if (methodSymbol.IsOverride)
        {
            return;
        }

        if (TryMethodConvention.IsInterfaceImplementation(methodSymbol))
        {
            return;
        }

        if (IsAllowedReturnType(methodSymbol.ReturnType))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.TryPrefixedMethodMustReturnTryResultType,
            methodDecl.Identifier.GetLocation(),
            methodName,
            methodSymbol.ReturnType.ToDisplayString()));
    }

    private static bool IsOnTryHelperType(IMethodSymbol methodSymbol)
    {
        var containingType = methodSymbol.ContainingType;
        return containingType is not null
               && containingType.Name == "Try"
               && containingType.ContainingNamespace?.ToDisplayString() == "NexusLabs.Framework";
    }

    private static bool IsAllowedReturnType(ITypeSymbol returnType)
    {
        if (returnType is INamedTypeSymbol named && named.IsGenericType)
        {
            var originalDefinition = named.OriginalDefinition.ToDisplayString();
            if (originalDefinition is
                    "System.Threading.Tasks.Task<TResult>" or
                    "System.Threading.Tasks.ValueTask<TResult>")
            {
                if (named.TypeArguments.Length == 1)
                {
                    return IsAllowedDirectReturnType(named.TypeArguments[0]);
                }

                return false;
            }
        }

        return IsAllowedDirectReturnType(returnType);
    }

    private static bool IsAllowedDirectReturnType(ITypeSymbol type)
    {
        var name = type.Name;

        if (name is "TriedEx" or "TriedNullEx")
        {
            return type.ContainingNamespace?.ToDisplayString() == "NexusLabs.Framework";
        }

        if (name == "Exception"
            && type.ContainingNamespace?.ToDisplayString() == "System")
        {
            return true;
        }

        return false;
    }
}
