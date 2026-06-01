using System.Collections.Immutable;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace NexusLabs.Framework.Analyzers;

/// <summary>
/// Flags <c>System.Threading.CancellationToken</c> parameters that carry a
/// default value. The rule applies to every method, constructor, local
/// function, and delegate signature that has a <c>CancellationToken</c>
/// parameter — no suffix-based scope, no kind-based filter, and no built-in
/// escape hatch beyond <c>#pragma</c> at the call site or project-level
/// <c>severity = none</c>. Suppress at the call site for intentional
/// ergonomic-default public APIs or <c>[EnumeratorCancellation]</c>
/// iterators.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class CancellationTokenDefaultAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.CancellationTokenMustNotHaveDefaultValue);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeParameter, SyntaxKind.Parameter);
    }

    private static void AnalyzeParameter(SyntaxNodeAnalysisContext context)
    {
        var parameter = (ParameterSyntax)context.Node;
        if (parameter.Default is null)
        {
            return;
        }

        var parameterSymbol = context.SemanticModel.GetDeclaredSymbol(
            parameter,
            context.CancellationToken);
        if (parameterSymbol is null)
        {
            return;
        }

        if (!IsCancellationToken(parameterSymbol.Type))
        {
            return;
        }

        var (containingTypeName, memberName) = ResolveOwningContext(parameterSymbol);
        if (containingTypeName is null || memberName is null)
        {
            return;
        }

        var defaultExpressionText = parameter.Default.Value.ToString();

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.CancellationTokenMustNotHaveDefaultValue,
            parameter.Default.GetLocation(),
            parameter.Identifier.Text,
            containingTypeName,
            memberName,
            defaultExpressionText));
    }

    private static bool IsCancellationToken(ITypeSymbol type)
    {
        return type.Name == "CancellationToken"
            && type.ContainingNamespace?.ToDisplayString() == "System.Threading";
    }

    private static (string? ContainingTypeName, string? MemberName) ResolveOwningContext(
        IParameterSymbol parameterSymbol)
    {
        var owner = parameterSymbol.ContainingSymbol;
        if (owner is null)
        {
            return (null, null);
        }

        var containingType = owner.ContainingType ?? owner as INamedTypeSymbol;
        if (containingType is null)
        {
            return (null, null);
        }

        var memberName = string.IsNullOrEmpty(owner.Name)
            ? containingType.Name
            : owner.Name;

        return (containingType.Name, memberName);
    }
}
