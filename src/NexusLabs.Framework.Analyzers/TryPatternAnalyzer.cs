using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

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
/// All checks are namespace-gated to <c>NexusLabs.Framework</c>.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TryPatternAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(
            DiagnosticDescriptors.MethodWithTryCatchShouldUseTryPattern,
            DiagnosticDescriptors.TryAsyncMethodScopeMustProvideLogger,
            DiagnosticDescriptors.ThrowInsideTryAsyncVariant);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
    }

    private static void AnalyzeMethod(SyntaxNodeAnalysisContext context)
    {
        var methodDeclaration = (MethodDeclarationSyntax)context.Node;
        var methodName = methodDeclaration.Identifier.Text;

        var semanticModel = context.SemanticModel;
        var methodSymbol = semanticModel.GetDeclaredSymbol(methodDeclaration, context.CancellationToken);

        if (IsInTryHelperClass(methodSymbol))
        {
            return;
        }

        if (methodSymbol?.IsExtensionMethod == true)
        {
            return;
        }

        if (methodDeclaration.Body is not null)
        {
            CheckForMethodWrappedInTryCatch(context, methodDeclaration, methodName);
        }

        CheckForTryAsyncUsage(context, methodDeclaration, methodName);
    }

    private static void CheckForMethodWrappedInTryCatch(
        SyntaxNodeAnalysisContext context,
        MethodDeclarationSyntax methodDeclaration,
        string methodName)
    {
        var body = methodDeclaration.Body;
        if (body is null)
        {
            return;
        }

        var methodSymbol = context.SemanticModel.GetDeclaredSymbol(methodDeclaration, context.CancellationToken);
        if (methodSymbol?.IsAsync != true)
        {
            return;
        }

        var statements = body.Statements;
        if (statements.Count != 1)
        {
            return;
        }

        if (statements[0] is not TryStatementSyntax tryStatement || tryStatement.Catches.Count == 0)
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.MethodWithTryCatchShouldUseTryPattern,
            methodDeclaration.Identifier.GetLocation(),
            methodName));
    }

    private static void CheckForTryAsyncUsage(
        SyntaxNodeAnalysisContext context,
        MethodDeclarationSyntax methodDeclaration,
        string methodName)
    {
        var semanticModel = context.SemanticModel;

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
            return;
        }

        var tryInvocations = invocations
            .Where(inv => IsTryAsyncVariant(inv, semanticModel, context.CancellationToken))
            .ToList();

        foreach (var invocation in tryInvocations)
        {
            var isMethodScoped = IsMethodScopedTryPattern(invocation, methodDeclaration);
            var isNestedInTryCallback = IsNestedInTryCallback(invocation, tryInvocations);

            if (isMethodScoped && !isNestedInTryCallback && !HasLoggerParameter(invocation))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.TryAsyncMethodScopeMustProvideLogger,
                    invocation.GetLocation(),
                    methodName));
            }

            CheckForThrowsInsideCallback(context, invocation, methodName);
        }
    }

    private static bool IsTryAsyncVariant(
        InvocationExpressionSyntax invocation,
        SemanticModel semanticModel,
        System.Threading.CancellationToken cancellationToken)
    {
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
        {
            return false;
        }

        var symbolInfo = semanticModel.GetSymbolInfo(memberAccess, cancellationToken);
        if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
        {
            return false;
        }

        if (!IsTryHelperType(methodSymbol.ContainingType))
        {
            return false;
        }

        return methodSymbol.Name is "Async" or "GetAsync" or "GetOrNullAsync";
    }

    private static bool IsInTryHelperClass(ISymbol? symbol)
    {
        if (symbol?.ContainingType is null)
        {
            return false;
        }

        return IsTryHelperType(symbol.ContainingType);
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

    private static bool IsMethodScopedTryPattern(
        InvocationExpressionSyntax invocation,
        MethodDeclarationSyntax methodDeclaration)
    {
        if (methodDeclaration.ExpressionBody is not null)
        {
            return methodDeclaration.ExpressionBody.DescendantNodesAndSelf().Contains(invocation);
        }

        if (methodDeclaration.Body is not null)
        {
            var statements = methodDeclaration.Body.Statements;
            if (statements.Count == 1 && statements[0] is ReturnStatementSyntax returnStatement)
            {
                return returnStatement.DescendantNodesAndSelf().Contains(invocation);
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

        // Logger-bearing overloads have at least (ILogger, callback). The no-logger
        // overloads take only a callback. Distinguishing on arg count is a structural
        // heuristic — matches the public Try.* surface.
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

            var throwStatements = argument.Expression.DescendantNodes().OfType<ThrowStatementSyntax>();
            foreach (var throwStatement in throwStatements)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.ThrowInsideTryAsyncVariant,
                    throwStatement.GetLocation(),
                    methodName));
            }
        }
    }

    private static bool IsNestedInTryCallback(
        InvocationExpressionSyntax invocation,
        List<InvocationExpressionSyntax> allTryInvocations)
    {
        foreach (var otherInvocation in allTryInvocations)
        {
            if (otherInvocation == invocation)
            {
                continue;
            }

            var callback = GetCallbackArgument(otherInvocation);
            if (callback is null)
            {
                continue;
            }

            if (callback.DescendantNodesAndSelf().Contains(invocation))
            {
                return true;
            }
        }

        return false;
    }

    private static SyntaxNode? GetCallbackArgument(InvocationExpressionSyntax invocation)
    {
        var arguments = invocation.ArgumentList?.Arguments;
        if (arguments is null || arguments.Value.Count == 0)
        {
            return null;
        }

        foreach (var argument in arguments.Value)
        {
            if (argument.Expression is ParenthesizedLambdaExpressionSyntax
                or SimpleLambdaExpressionSyntax
                or AnonymousMethodExpressionSyntax)
            {
                return argument.Expression;
            }
        }

        return null;
    }
}
