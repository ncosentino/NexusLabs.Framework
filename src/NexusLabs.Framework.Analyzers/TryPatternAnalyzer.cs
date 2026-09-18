using System.Collections.Immutable;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace NexusLabs.Framework.Analyzers;

/// <summary>
/// Enforces correct usage of the <c>NexusLabs.Framework.Try</c> orchestration
/// helpers. Three diagnostics:
/// <list type="bullet">
///   <item><c>NLF0006</c>: async method's entire body is a single try-catch — should
///         use <c>Try.Async</c> / <c>Try.GetAsync</c> / <c>Try.GetOrNullAsync</c>
///         instead.</item>
///   <item><c>NLF0007</c>: <c>Try.Async</c> variants used at method scope must
///         receive an <c>ILogger</c> (single-argument overloads exist for non-
///         method-scoped helper usage but the method-scoped pattern always wants
///         logging).</item>
///   <item><c>NLF0008</c>: <c>throw</c> statement found inside a
///         <c>Try.Async</c> callback — the Try helpers expect callbacks to
///         <em>return</em> exceptions (via <c>TriedEx&lt;T&gt;</c>), not throw
///         them.</item>
/// </list>
/// All checks are symbol-gated to <c>NexusLabs.Framework.Try</c>.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TryPatternAnalyzer : DiagnosticAnalyzer
{
    private const string TryHelperMetadataName = "NexusLabs.Framework.Try";

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(
            DiagnosticDescriptors.MethodWithTryCatchShouldUseTryPattern,
            DiagnosticDescriptors.TryAsyncMethodScopeMustProvideLogger,
            DiagnosticDescriptors.ThrowInsideTryAsyncVariant);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(compilationContext =>
        {
            var tryHelperType = compilationContext.Compilation
                .GetTypeByMetadataName(TryHelperMetadataName);
            if (tryHelperType is null)
            {
                return;
            }

            compilationContext.RegisterSyntaxNodeAction(
                syntaxContext => AnalyzeTryStatement(syntaxContext, tryHelperType),
                SyntaxKind.TryStatement);

            compilationContext.RegisterSyntaxNodeAction(
                syntaxContext => AnalyzeInvocation(syntaxContext, tryHelperType),
                SyntaxKind.InvocationExpression);
        });
    }

    private static void AnalyzeTryStatement(
        SyntaxNodeAnalysisContext context,
        INamedTypeSymbol tryHelperType)
    {
        var tryStatement = (TryStatementSyntax)context.Node;
        if (tryStatement.Catches.Count == 0 ||
            tryStatement.Parent is not BlockSyntax body ||
            body.Parent is not MethodDeclarationSyntax methodDeclaration ||
            body.Statements.Count != 1 ||
            !HasModifier(methodDeclaration.Modifiers, SyntaxKind.AsyncKeyword))
        {
            return;
        }

        var methodSymbol = context.SemanticModel.GetDeclaredSymbol(
            methodDeclaration,
            context.CancellationToken);
        if (methodSymbol is null ||
            methodSymbol.IsExtensionMethod ||
            IsInTryHelperClass(methodSymbol, tryHelperType))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.MethodWithTryCatchShouldUseTryPattern,
            methodDeclaration.Identifier.GetLocation(),
            methodDeclaration.Identifier.ValueText));
    }

    private static void AnalyzeInvocation(
        SyntaxNodeAnalysisContext context,
        INamedTypeSymbol tryHelperType)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        if (!IsTryAsyncVariant(
            invocation,
            context.SemanticModel,
            tryHelperType,
            context.CancellationToken))
        {
            return;
        }

        var methodDeclaration = invocation.FirstAncestorOrSelf<MethodDeclarationSyntax>();
        if (methodDeclaration is null ||
            IsExtensionMethod(methodDeclaration) ||
            IsInTryHelperClass(context.ContainingSymbol, tryHelperType))
        {
            return;
        }

        var methodName = methodDeclaration.Identifier.ValueText;
        if (IsMethodScopedTryPattern(invocation, methodDeclaration) &&
            !HasLoggerParameter(invocation) &&
            !IsNestedInTryCallback(
                invocation,
                context.SemanticModel,
                tryHelperType,
                context.CancellationToken))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.TryAsyncMethodScopeMustProvideLogger,
                invocation.GetLocation(),
                methodName));
        }

        CheckForThrowsInsideCallback(context, invocation, methodName);
    }

    private static bool IsTryAsyncVariant(
        InvocationExpressionSyntax invocation,
        SemanticModel semanticModel,
        INamedTypeSymbol tryHelperType,
        CancellationToken cancellationToken)
    {
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
        {
            return false;
        }

        var methodName = memberAccess.Name.Identifier.ValueText;
        if (methodName is not ("Async" or "GetAsync" or "GetOrNullAsync"))
        {
            return false;
        }

        var symbolInfo = semanticModel.GetSymbolInfo(memberAccess, cancellationToken);
        if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
        {
            return false;
        }

        return SymbolEqualityComparer.Default.Equals(
            methodSymbol.ContainingType,
            tryHelperType);
    }

    private static bool IsInTryHelperClass(
        ISymbol? symbol,
        INamedTypeSymbol tryHelperType) =>
        SymbolEqualityComparer.Default.Equals(
            symbol?.ContainingType,
            tryHelperType);

    private static bool IsMethodScopedTryPattern(
        InvocationExpressionSyntax invocation,
        MethodDeclarationSyntax methodDeclaration)
    {
        if (methodDeclaration.ExpressionBody is not null)
        {
            return methodDeclaration.ExpressionBody.Expression.Span.Contains(invocation.Span);
        }

        if (methodDeclaration.Body is not null)
        {
            var statements = methodDeclaration.Body.Statements;
            if (statements.Count == 1 && statements[0] is ReturnStatementSyntax returnStatement)
            {
                return returnStatement.Expression?.Span.Contains(invocation.Span) == true;
            }
        }

        return false;
    }

    private static bool HasLoggerParameter(InvocationExpressionSyntax invocation)
    {
        var arguments = invocation.ArgumentList?.Arguments;
        if (arguments is null || arguments.Value.Count == 0)
        {
            return false;
        }

        return arguments.Value.Count >= 2;
    }

    private static void CheckForThrowsInsideCallback(
        SyntaxNodeAnalysisContext context,
        InvocationExpressionSyntax invocation,
        string methodName)
    {
        var arguments = invocation.ArgumentList?.Arguments;
        if (arguments is null)
        {
            return;
        }

        foreach (var argument in arguments.Value)
        {
            if (argument.Expression is not (ParenthesizedLambdaExpressionSyntax
                or SimpleLambdaExpressionSyntax
                or AnonymousMethodExpressionSyntax))
            {
                continue;
            }

            foreach (var descendant in argument.Expression.DescendantNodes())
            {
                if (descendant is not ThrowStatementSyntax throwStatement)
                {
                    continue;
                }

                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.ThrowInsideTryAsyncVariant,
                    throwStatement.GetLocation(),
                    methodName));
            }
        }
    }

    private static bool IsNestedInTryCallback(
        InvocationExpressionSyntax invocation,
        SemanticModel semanticModel,
        INamedTypeSymbol tryHelperType,
        CancellationToken cancellationToken)
    {
        foreach (var ancestor in invocation.Ancestors())
        {
            if (ancestor is not AnonymousFunctionExpressionSyntax callback ||
                callback.Parent is not ArgumentSyntax argument ||
                argument.Parent?.Parent is not InvocationExpressionSyntax callbackInvocation)
            {
                continue;
            }

            if (IsTryAsyncVariant(
                callbackInvocation,
                semanticModel,
                tryHelperType,
                cancellationToken))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsExtensionMethod(MethodDeclarationSyntax methodDeclaration)
    {
        var parameters = methodDeclaration.ParameterList.Parameters;
        if (parameters.Count == 0)
        {
            return false;
        }

        return HasModifier(parameters[0].Modifiers, SyntaxKind.ThisKeyword);
    }

    private static bool HasModifier(
        SyntaxTokenList modifiers,
        SyntaxKind kind)
    {
        foreach (var modifier in modifiers)
        {
            if (modifier.IsKind(kind))
            {
                return true;
            }
        }

        return false;
    }
}
