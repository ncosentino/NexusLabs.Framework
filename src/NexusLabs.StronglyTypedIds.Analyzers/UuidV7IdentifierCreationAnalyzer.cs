using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace NexusLabs.StronglyTypedIds.Analyzers;

/// <summary>
/// Rejects UUIDv4-producing creation paths for identifiers that implement
/// <c>NexusLabs.StronglyTypedIds.IUuidV7Identifier&lt;T&gt;</c>.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UuidV7IdentifierCreationAnalyzer : DiagnosticAnalyzer
{
    private const string UuidV7IdentifierInterfaceName = "IUuidV7Identifier";

    private const string UuidV7IdentifierNamespace = "NexusLabs.StronglyTypedIds";

    private const string GeneratedCodeToolName = "StronglyTypedId";

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(
            DiagnosticDescriptors.UseUuidV7CreateInsteadOfNew,
            DiagnosticDescriptors.DoNotConstructUuidV7IdFromNewGuid);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
        context.RegisterOperationAction(AnalyzeMethodReference, OperationKind.MethodReference);
        context.RegisterOperationAction(AnalyzeObjectCreation, OperationKind.ObjectCreation);
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context)
    {
        var invocation = (IInvocationOperation)context.Operation;
        ReportNewUsage(context, invocation.TargetMethod, invocation.Syntax);
    }

    private static void AnalyzeMethodReference(OperationAnalysisContext context)
    {
        var methodReference = (IMethodReferenceOperation)context.Operation;
        if (methodReference.Parent is IInvocationOperation)
        {
            return;
        }

        ReportNewUsage(context, methodReference.Method, methodReference.Syntax);
    }

    private static void AnalyzeObjectCreation(OperationAnalysisContext context)
    {
        var creation = (IObjectCreationOperation)context.Operation;
        if (creation.Type is not INamedTypeSymbol identifierType ||
            !IsUuidV7Identifier(identifierType) ||
            creation.Arguments.Length != 1)
        {
            return;
        }

        var argument = Unwrap(creation.Arguments[0].Value);
        if (argument is not IInvocationOperation invocation ||
            !IsGuidNewGuid(invocation.TargetMethod))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.DoNotConstructUuidV7IdFromNewGuid,
            creation.Syntax.GetLocation(),
            identifierType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
    }

    private static void ReportNewUsage(
        OperationAnalysisContext context,
        IMethodSymbol method,
        SyntaxNode syntax)
    {
        if (!method.IsStatic ||
            method.Name != "New" ||
            method.Arity != 0 ||
            method.Parameters.Length != 0 ||
            method.ContainingType is not { } identifierType ||
            !SymbolEqualityComparer.Default.Equals(method.ReturnType, identifierType) ||
            !IsGeneratedIdentifier(identifierType) ||
            !IsUuidV7Identifier(identifierType))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.UseUuidV7CreateInsteadOfNew,
            GetMemberNameLocation(syntax),
            identifierType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
    }

    private static bool IsUuidV7Identifier(INamedTypeSymbol type)
    {
        foreach (var implementedInterface in type.AllInterfaces)
        {
            if (implementedInterface.Arity != 1 ||
                implementedInterface.Name != UuidV7IdentifierInterfaceName ||
                implementedInterface.ContainingNamespace?.ToDisplayString() !=
                    UuidV7IdentifierNamespace)
            {
                continue;
            }

            if (SymbolEqualityComparer.Default.Equals(
                    implementedInterface.TypeArguments[0],
                    type))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsGuidNewGuid(IMethodSymbol method) =>
        method.IsStatic &&
        method.Name == "NewGuid" &&
        method.Parameters.Length == 0 &&
        method.ContainingType?.Name == "Guid" &&
        method.ContainingNamespace?.ToDisplayString() == "System";

    private static bool IsGeneratedIdentifier(INamedTypeSymbol type)
    {
        foreach (var attribute in type.GetAttributes())
        {
            if (attribute.AttributeClass?.Name == "GeneratedCodeAttribute" &&
                attribute.AttributeClass.ContainingNamespace?.ToDisplayString() ==
                    "System.CodeDom.Compiler" &&
                attribute.ConstructorArguments.Length > 0 &&
                attribute.ConstructorArguments[0].Value is
                    GeneratedCodeToolName)
            {
                return true;
            }
        }

        return false;
    }

    private static IOperation Unwrap(IOperation operation)
    {
        while (true)
        {
            switch (operation)
            {
                case IConversionOperation conversion:
                    operation = conversion.Operand;
                    continue;

                case IParenthesizedOperation parenthesized:
                    operation = parenthesized.Operand;
                    continue;

                default:
                    return operation;
            }
        }
    }

    private static Location GetMemberNameLocation(SyntaxNode syntax) =>
        syntax switch
        {
            InvocationExpressionSyntax
            {
                Expression: MemberAccessExpressionSyntax memberAccess
            } => memberAccess.Name.GetLocation(),
            InvocationExpressionSyntax
            {
                Expression: SimpleNameSyntax simpleName
            } => simpleName.GetLocation(),
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.GetLocation(),
            SimpleNameSyntax simpleName => simpleName.GetLocation(),
            _ => syntax.GetLocation(),
        };
}
