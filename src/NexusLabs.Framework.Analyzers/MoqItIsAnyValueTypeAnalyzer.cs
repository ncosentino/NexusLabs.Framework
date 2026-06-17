using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace NexusLabs.Framework.Analyzers;

/// <summary>
/// Flags <c>Moq.It.IsAny&lt;T&gt;()</c> where <c>T</c> is a value type (other
/// than <c>System.Threading.CancellationToken</c>) or a record. For such types
/// the exact value usually IS the thing a test should assert, so the caller
/// should match the expected value directly or use <c>It.Is&lt;T&gt;(x =&gt; ...)</c>.
/// Reference types and open generic type parameters are unaffected.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MoqItIsAnyValueTypeAnalyzer : DiagnosticAnalyzer
{
    private static readonly SymbolDisplayFormat TypeNameFormat =
        SymbolDisplayFormat.MinimallyQualifiedFormat;

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.MoqItIsAnyOnValueTypeOrRecord);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        if (context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol
            is not IMethodSymbol method)
        {
            return;
        }

        if (method.Name != "IsAny"
            || !IsMoqItType(method.ContainingType)
            || method.TypeArguments.Length != 1)
        {
            return;
        }

        var typeArgument = method.TypeArguments[0];

        if (typeArgument is ITypeParameterSymbol || IsCancellationToken(typeArgument))
        {
            return;
        }

        string? kind = null;
        if (typeArgument.IsRecord)
        {
            kind = "record";
        }
        else if (typeArgument.IsValueType)
        {
            kind = "value type";
        }

        if (kind is null)
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.MoqItIsAnyOnValueTypeOrRecord,
            invocation.GetLocation(),
            typeArgument.ToDisplayString(TypeNameFormat),
            kind));
    }

    private static bool IsMoqItType(INamedTypeSymbol? type)
    {
        return type is { Name: "It" }
            && type.ContainingNamespace?.ToDisplayString() == "Moq";
    }

    private static bool IsCancellationToken(ITypeSymbol type)
    {
        return type.Name == "CancellationToken"
            && type.ContainingNamespace?.ToDisplayString() == "System.Threading";
    }
}
