using System.Collections.Immutable;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace NexusLabs.Framework.Analyzers;

/// <summary>
/// Flags Moq mocks that use a non-<c>Strict</c> <c>MockBehavior</c>. Fires on a
/// <c>Moq.MockRepository</c> constructed with a non-Strict behavior and on a
/// <c>repository.Create&lt;T&gt;(MockBehavior.Loose/Default, ...)</c> override
/// that downgrades the behavior. Direct <c>new Mock&lt;T&gt;(...)</c> is owned by
/// NLF0021 (which forbids it outright), so this rule does not also flag it.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MoqMockBehaviorStrictAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.MoqMockBehaviorMustBeStrict);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(
            AnalyzeObjectCreation,
            SyntaxKind.ObjectCreationExpression,
            SyntaxKind.ImplicitObjectCreationExpression);
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context)
    {
        var objectCreation = (BaseObjectCreationExpressionSyntax)context.Node;

        if (context.SemanticModel.GetTypeInfo(objectCreation, context.CancellationToken).Type
            is not INamedTypeSymbol type)
        {
            return;
        }

        if (!IsMoqMockRepository(type) || objectCreation.ArgumentList is null)
        {
            return;
        }

        ReportFirstNonStrictArgument(context, objectCreation.ArgumentList);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        if (context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol
            is not IMethodSymbol method)
        {
            return;
        }

        if (method.Name != "Create"
            || !IsMoqRepositoryOrFactory(method.ContainingType)
            || invocation.ArgumentList is null)
        {
            return;
        }

        ReportFirstNonStrictArgument(context, invocation.ArgumentList);
    }

    private static void ReportFirstNonStrictArgument(
        SyntaxNodeAnalysisContext context,
        ArgumentListSyntax argumentList)
    {
        foreach (var argument in argumentList.Arguments)
        {
            if (!TryGetNonStrictBehaviorName(
                    argument.Expression,
                    context.SemanticModel,
                    context.CancellationToken,
                    out var behaviorName))
            {
                continue;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.MoqMockBehaviorMustBeStrict,
                argument.Expression.GetLocation(),
                argument.Expression.ToString(),
                behaviorName));
            return;
        }
    }

    private static bool TryGetNonStrictBehaviorName(
        ExpressionSyntax expression,
        SemanticModel semanticModel,
        CancellationToken cancellationToken,
        out string behaviorName)
    {
        behaviorName = string.Empty;

        if (semanticModel.GetSymbolInfo(expression, cancellationToken).Symbol
            is not IFieldSymbol field)
        {
            return false;
        }

        if (!IsMoqMockBehavior(field.ContainingType) || field.Name == "Strict")
        {
            return false;
        }

        behaviorName = field.Name;
        return true;
    }

    private static bool IsMoqMockRepository(INamedTypeSymbol type)
    {
        return type is { Name: "MockRepository" }
            && type.ContainingNamespace?.ToDisplayString() == "Moq";
    }

    private static bool IsMoqRepositoryOrFactory(INamedTypeSymbol? type)
    {
        return type is not null
            && type.Name is "MockRepository" or "MockFactory"
            && type.ContainingNamespace?.ToDisplayString() == "Moq";
    }

    private static bool IsMoqMockBehavior(INamedTypeSymbol? type)
    {
        return type is { Name: "MockBehavior" }
            && type.ContainingNamespace?.ToDisplayString() == "Moq";
    }
}
