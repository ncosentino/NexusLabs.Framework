using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace NexusLabs.Framework.Analyzers;

/// <summary>
/// Requires a Try prefix on methods returning framework error results, except overrides
/// and implementations of interfaces declared outside the current compilation's assembly.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TryResultMethodNameAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.TryResultMethodMustHaveTryPrefix);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(compilationContext =>
        {
            var compilation = compilationContext.Compilation;
            var triedEx = compilation.GetTypeByMetadataName("NexusLabs.Framework.TriedEx`1");
            var triedNullEx = compilation.GetTypeByMetadataName("NexusLabs.Framework.TriedNullEx`1");
            var exception = compilation.GetTypeByMetadataName("System.Exception");
            var task = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1");
            var valueTask = compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask`1");

            compilationContext.RegisterSyntaxNodeAction(
                nodeContext => AnalyzeMethod(nodeContext, triedEx, triedNullEx, exception, task, valueTask),
                SyntaxKind.MethodDeclaration);
        });
    }

    private static void AnalyzeMethod(
        SyntaxNodeAnalysisContext context,
        INamedTypeSymbol? triedEx,
        INamedTypeSymbol? triedNullEx,
        INamedTypeSymbol? exception,
        INamedTypeSymbol? task,
        INamedTypeSymbol? valueTask)
    {
        var declaration = (MethodDeclarationSyntax)context.Node;
        var methodName = declaration.Identifier.ValueText;
        if (TryMethodConvention.IsTryPrefixed(methodName))
        {
            return;
        }

        var method = context.SemanticModel.GetDeclaredSymbol(declaration, context.CancellationToken);
        if (method is null || method.IsOverride)
        {
            return;
        }

        var returnType = method.ReturnType;
        if (returnType is INamedTypeSymbol named
            && named.TypeArguments.Length == 1
            && (SymbolEqualityComparer.Default.Equals(named.OriginalDefinition, task)
                || SymbolEqualityComparer.Default.Equals(named.OriginalDefinition, valueTask)))
        {
            returnType = named.TypeArguments[0];
        }

        if (!SymbolEqualityComparer.Default.Equals(returnType.OriginalDefinition, triedEx)
            && !SymbolEqualityComparer.Default.Equals(returnType.OriginalDefinition, triedNullEx)
            && !(SymbolEqualityComparer.Default.Equals(returnType, exception)
                && returnType.NullableAnnotation == NullableAnnotation.Annotated))
        {
            return;
        }

        if (TryMethodConvention.IsInterfaceImplementation(method, context.Compilation.Assembly))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.TryResultMethodMustHaveTryPrefix,
            declaration.Identifier.GetLocation(),
            methodName));
    }
}
