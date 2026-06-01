using System.Collections.Immutable;
using System.Linq;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace NexusLabs.Framework.Analyzers;

/// <summary>
/// Flags <c>HashSet&lt;string&gt;</c> creations and <c>ToHashSet</c> calls that
/// do not pass <c>StringComparer.OrdinalIgnoreCase</c>. Catches:
/// <list type="bullet">
///   <item><c>new HashSet&lt;string&gt;()</c> and overloads with capacity /
///         <c>IEnumerable&lt;string&gt;</c> but no comparer argument</item>
///   <item><c>new HashSet&lt;string&gt;(StringComparer.Ordinal)</c> and other
///         non-<c>OrdinalIgnoreCase</c> comparers</item>
///   <item>target-typed <c>HashSet&lt;string&gt; s = new();</c></item>
///   <item><c>source.ToHashSet()</c> where source is
///         <c>IEnumerable&lt;string&gt;</c>, with or without a non-
///         <c>OrdinalIgnoreCase</c> comparer</item>
/// </list>
/// <c>HashSet&lt;T&gt;</c> for non-string <c>T</c> is unaffected.
/// <c>ImmutableHashSet&lt;string&gt;</c> is a separate type and is not covered.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class HashSetOfStringComparerAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.HashSetOfStringMustUseOrdinalIgnoreCase);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeObjectCreation, SyntaxKind.ObjectCreationExpression);
        context.RegisterSyntaxNodeAction(AnalyzeImplicitObjectCreation, SyntaxKind.ImplicitObjectCreationExpression);
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context)
    {
        var creation = (ObjectCreationExpressionSyntax)context.Node;
        var typeInfo = context.SemanticModel.GetTypeInfo(creation, context.CancellationToken);

        if (typeInfo.Type is not INamedTypeSymbol namedType ||
            !IsHashSetOfString(namedType))
        {
            return;
        }

        if (HasOrdinalIgnoreCaseComparerArgument(creation.ArgumentList, context.SemanticModel, context.CancellationToken))
        {
            return;
        }

        var location = creation.Type.GetLocation();
        var snippet = "new " + creation.Type.ToString() + "(...)";

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.HashSetOfStringMustUseOrdinalIgnoreCase,
            location,
            snippet));
    }

    private static void AnalyzeImplicitObjectCreation(SyntaxNodeAnalysisContext context)
    {
        var creation = (ImplicitObjectCreationExpressionSyntax)context.Node;
        var typeInfo = context.SemanticModel.GetTypeInfo(creation, context.CancellationToken);

        if (typeInfo.Type is not INamedTypeSymbol namedType ||
            !IsHashSetOfString(namedType))
        {
            return;
        }

        if (HasOrdinalIgnoreCaseComparerArgument(creation.ArgumentList, context.SemanticModel, context.CancellationToken))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.HashSetOfStringMustUseOrdinalIgnoreCase,
            creation.NewKeyword.GetLocation(),
            "target-typed `new()` HashSet<string>"));
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        if (context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol is not IMethodSymbol method)
        {
            return;
        }

        if (!IsToHashSetExtension(method))
        {
            return;
        }

        if (method.ReturnType is not INamedTypeSymbol returnType ||
            !IsHashSetOfString(returnType))
        {
            return;
        }

        if (HasOrdinalIgnoreCaseComparerArgument(invocation.ArgumentList, context.SemanticModel, context.CancellationToken))
        {
            return;
        }

        var location = GetInvocationNameLocation(invocation) ?? invocation.GetLocation();

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.HashSetOfStringMustUseOrdinalIgnoreCase,
            location,
            "ToHashSet() on IEnumerable<string>"));
    }

    private static bool IsHashSetOfString(INamedTypeSymbol namedType)
    {
        if (!namedType.IsGenericType)
        {
            return false;
        }

        var original = namedType.OriginalDefinition.ToDisplayString();
        if (original != "System.Collections.Generic.HashSet<T>")
        {
            return false;
        }

        if (namedType.TypeArguments.Length != 1)
        {
            return false;
        }

        return namedType.TypeArguments[0].SpecialType == SpecialType.System_String;
    }

    private static bool IsToHashSetExtension(IMethodSymbol method)
    {
        if (method.Name != "ToHashSet")
        {
            return false;
        }

        if (!method.IsExtensionMethod)
        {
            return false;
        }

        var containingType = method.ContainingType;
        if (containingType is null)
        {
            return false;
        }

        return containingType.Name == "Enumerable"
            && containingType.ContainingNamespace?.ToDisplayString() == "System.Linq";
    }

    private static bool HasOrdinalIgnoreCaseComparerArgument(
        ArgumentListSyntax? argumentList,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        if (argumentList is null || argumentList.Arguments.Count == 0)
        {
            return false;
        }

        foreach (var argument in argumentList.Arguments)
        {
            if (IsOrdinalIgnoreCaseComparer(argument.Expression, semanticModel, cancellationToken))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsOrdinalIgnoreCaseComparer(
        ExpressionSyntax expression,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        if (expression is not MemberAccessExpressionSyntax memberAccess)
        {
            return false;
        }

        var symbol = semanticModel.GetSymbolInfo(memberAccess, cancellationToken).Symbol;
        var containerName = symbol switch
        {
            IPropertySymbol prop when prop.Name == "OrdinalIgnoreCase" => prop.ContainingType?.Name,
            IFieldSymbol field when field.Name == "OrdinalIgnoreCase" => field.ContainingType?.Name,
            _ => null,
        };

        return containerName == "StringComparer";
    }

    private static Location? GetInvocationNameLocation(InvocationExpressionSyntax invocation)
    {
        return invocation.Expression switch
        {
            MemberAccessExpressionSyntax member => member.Name.GetLocation(),
            GenericNameSyntax generic => generic.Identifier.GetLocation(),
            IdentifierNameSyntax id => id.GetLocation(),
            _ => null,
        };
    }
}
