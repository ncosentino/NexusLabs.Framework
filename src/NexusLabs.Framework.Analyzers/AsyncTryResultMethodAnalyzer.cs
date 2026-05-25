using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace NexusLabs.Framework.Analyzers;

/// <summary>
/// Flags <c>async</c> methods whose return type is
/// <c>Task&lt;TriedEx&lt;T&gt;&gt;</c> or <c>Task&lt;TriedNullEx&lt;T&gt;&gt;</c>
/// but whose body neither directly delegates to another method returning the
/// same shape nor wraps via <c>Try.GetAsync</c> / <c>Try.GetOrNullAsync</c>.
/// Such methods will silently fail to catch their own exceptions — the Task
/// will fault instead of resolving to a TriedEx with Error set. Both the Try
/// helper class and the TriedEx / TriedNullEx types must come from
/// <c>NexusLabs.Framework</c> namespace.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AsyncTryResultMethodAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.AsyncMethodReturningTryResultShouldUseTryPattern);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
    }

    private static void AnalyzeMethod(SyntaxNodeAnalysisContext context)
    {
        var methodDeclaration = (MethodDeclarationSyntax)context.Node;
        var semanticModel = context.SemanticModel;
        var methodSymbol = semanticModel.GetDeclaredSymbol(methodDeclaration, context.CancellationToken);

        if (methodSymbol is null)
        {
            return;
        }

        if (IsTryHelperType(methodSymbol.ContainingType))
        {
            return;
        }

        if (!methodSymbol.IsAsync)
        {
            return;
        }

        if (methodSymbol.IsExtensionMethod)
        {
            return;
        }

        if (methodSymbol.ReturnType is not INamedTypeSymbol returnType)
        {
            return;
        }

        if (returnType.Name != "Task" || !returnType.IsGenericType || returnType.TypeArguments.Length != 1)
        {
            return;
        }

        var wrappedType = returnType.TypeArguments[0];
        if (wrappedType is not INamedTypeSymbol namedWrappedType || !namedWrappedType.IsGenericType)
        {
            return;
        }

        if (!IsTryResultType(namedWrappedType.ConstructedFrom))
        {
            return;
        }

        if (IsDirectPassThrough(methodDeclaration, semanticModel, namedWrappedType, context.CancellationToken))
        {
            return;
        }

        if (UsesTryPattern(methodDeclaration, semanticModel, context.CancellationToken))
        {
            return;
        }

        var returnTypeDisplay = $"Task<{namedWrappedType.Name}<{namedWrappedType.TypeArguments[0].Name}>>";
        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.AsyncMethodReturningTryResultShouldUseTryPattern,
            methodDeclaration.Identifier.GetLocation(),
            methodSymbol.Name,
            returnTypeDisplay));
    }

    private static bool IsDirectPassThrough(
        MethodDeclarationSyntax methodDeclaration,
        SemanticModel semanticModel,
        INamedTypeSymbol expectedReturnType,
        System.Threading.CancellationToken cancellationToken)
    {
        if (methodDeclaration.ExpressionBody is not null)
        {
            return IsAwaitingTryResultCall(methodDeclaration.ExpressionBody.Expression, semanticModel, expectedReturnType, cancellationToken);
        }

        if (methodDeclaration.Body is not null && methodDeclaration.Body.Statements.Count == 1)
        {
            if (methodDeclaration.Body.Statements[0] is ReturnStatementSyntax returnStatement &&
                returnStatement.Expression is not null)
            {
                return IsAwaitingTryResultCall(returnStatement.Expression, semanticModel, expectedReturnType, cancellationToken);
            }
        }

        return false;
    }

    private static bool IsAwaitingTryResultCall(
        ExpressionSyntax expression,
        SemanticModel semanticModel,
        INamedTypeSymbol expectedReturnType,
        System.Threading.CancellationToken cancellationToken)
    {
        if (expression is AwaitExpressionSyntax awaitExpression)
        {
            return IsTryResultCall(awaitExpression.Expression, semanticModel, expectedReturnType, cancellationToken);
        }

        return IsTryResultCall(expression, semanticModel, expectedReturnType, cancellationToken);
    }

    private static bool IsTryResultCall(
        ExpressionSyntax expression,
        SemanticModel semanticModel,
        INamedTypeSymbol expectedReturnType,
        System.Threading.CancellationToken cancellationToken)
    {
        var typeInfo = semanticModel.GetTypeInfo(expression, cancellationToken);
        if (typeInfo.Type is not INamedTypeSymbol callReturnType)
        {
            return false;
        }

        if (callReturnType.Name != "Task" || !callReturnType.IsGenericType || callReturnType.TypeArguments.Length != 1)
        {
            return false;
        }

        if (callReturnType.TypeArguments[0] is not INamedTypeSymbol namedWrappedType)
        {
            return false;
        }

        return IsTryResultType(namedWrappedType.ConstructedFrom) &&
               SymbolEqualityComparer.Default.Equals(namedWrappedType.ConstructedFrom, expectedReturnType.ConstructedFrom);
    }

    private static bool UsesTryPattern(
        MethodDeclarationSyntax methodDeclaration,
        SemanticModel semanticModel,
        System.Threading.CancellationToken cancellationToken)
    {
        IEnumerable<InvocationExpressionSyntax> invocations;
        if (methodDeclaration.Body is not null)
        {
            invocations = methodDeclaration.Body.DescendantNodes().OfType<InvocationExpressionSyntax>();
        }
        else if (methodDeclaration.ExpressionBody is not null)
        {
            invocations = methodDeclaration.ExpressionBody.DescendantNodes().OfType<InvocationExpressionSyntax>();
        }
        else
        {
            return false;
        }

        foreach (var invocation in invocations)
        {
            if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            {
                continue;
            }

            var symbolInfo = semanticModel.GetSymbolInfo(memberAccess, cancellationToken);
            if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
            {
                continue;
            }

            if (!IsTryHelperType(methodSymbol.ContainingType))
            {
                continue;
            }

            if (methodSymbol.Name is "GetAsync" or "GetOrNullAsync")
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsTryHelperType(INamedTypeSymbol? type)
    {
        if (type is null)
        {
            return false;
        }

        return type.Name == "Try" &&
               type.ContainingNamespace?.ToDisplayString() == "NexusLabs.Framework";
    }

    private static bool IsTryResultType(INamedTypeSymbol? type)
    {
        if (type is null)
        {
            return false;
        }

        return type.Name is "TriedEx" or "TriedNullEx" &&
               type.ContainingNamespace?.ToDisplayString() == "NexusLabs.Framework";
    }
}
