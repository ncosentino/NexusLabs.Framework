using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace NexusLabs.Framework.Analyzers;

/// <summary>
/// Flags <c>System.Console.Write</c>, <c>System.Console.WriteLine</c>,
/// <c>System.Diagnostics.Debug.Write</c>, and <c>System.Diagnostics.Debug.WriteLine</c>
/// invocations. Opt out per-project via
/// <c>dotnet_diagnostic.NLF0001.severity = none</c> in <c>.editorconfig</c>.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ConsoleWriteAnalyzer : DiagnosticAnalyzer
{
    private static readonly ImmutableHashSet<string> ForbiddenMethods = ImmutableHashSet.Create(
        StringComparer.Ordinal,
        "System.Console.Write",
        "System.Console.WriteLine",
        "System.Diagnostics.Debug.Write",
        "System.Diagnostics.Debug.WriteLine");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.DoNotUseConsoleWrite);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        if (context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol is not IMethodSymbol method)
        {
            return;
        }

        var fullyQualified = $"{method.ContainingType.ToDisplayString()}.{method.Name}";
        if (!ForbiddenMethods.Contains(fullyQualified))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.DoNotUseConsoleWrite,
            invocation.GetLocation(),
            fullyQualified));
    }
}
