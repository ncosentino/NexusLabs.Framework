using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace NexusLabs.Framework.Analyzers;

/// <summary>
/// Flags direct Moq mock creation — <c>new Mock&lt;T&gt;(...)</c> and
/// <c>Mock.Of&lt;T&gt;(...)</c> — both of which create a mock outside any
/// <c>MockRepository</c>. Mocks in this codebase must be created from a shared
/// <c>MockRepository</c> (see NLF0022 for the strict-behavior requirement) so
/// every mock shares one behavior and a single <c>VerifyAll()</c> can assert
/// all setups. Matches <c>Moq.Mock&lt;T&gt;</c> and the static
/// <c>Moq.Mock.Of</c> factory by namespace + name.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MoqMockFromRepositoryAnalyzer : DiagnosticAnalyzer
{
    private static readonly SymbolDisplayFormat TypeNameFormat =
        SymbolDisplayFormat.MinimallyQualifiedFormat;

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.MoqMockMustComeFromRepository);

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

        if (!IsMoqGenericMock(type))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.MoqMockMustComeFromRepository,
            objectCreation.GetLocation(),
            $"new {type.ToDisplayString(TypeNameFormat)}()"));
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        if (context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol
            is not IMethodSymbol method)
        {
            return;
        }

        if (method.Name != "Of" || !IsMoqStaticMockType(method.ContainingType))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.MoqMockMustComeFromRepository,
            invocation.GetLocation(),
            $"{invocation.Expression}()"));
    }

    private static bool IsMoqGenericMock(INamedTypeSymbol type)
    {
        return type is { Name: "Mock", IsGenericType: true }
            && type.ContainingNamespace?.ToDisplayString() == "Moq";
    }

    private static bool IsMoqStaticMockType(INamedTypeSymbol? type)
    {
        return type is { Name: "Mock" }
            && type.ContainingNamespace?.ToDisplayString() == "Moq";
    }
}
