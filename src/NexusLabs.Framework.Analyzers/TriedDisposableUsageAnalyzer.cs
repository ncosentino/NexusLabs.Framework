using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace NexusLabs.Framework.Analyzers;

/// <summary>
/// Flags locals of <c>TriedEx&lt;T&gt;</c> / <c>TriedNullEx&lt;T&gt;</c> / <c>Tried&lt;T&gt;</c>
/// where <c>T</c> implements <see cref="System.IDisposable"/> or <see cref="System.IAsyncDisposable"/>
/// and the declaration is not a <c>using</c> declaration, the local is not returned, not passed as
/// an argument, not assigned to a field/property, and never has <c>Dispose</c>/<c>DisposeAsync</c>
/// invoked on it (NLF0011).
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TriedDisposableUsageAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.TriedDisposableValueNotDisposed);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeLocalDeclaration, SyntaxKind.LocalDeclarationStatement);
    }

    private static void AnalyzeLocalDeclaration(SyntaxNodeAnalysisContext context)
    {
        var localDeclaration = (LocalDeclarationStatementSyntax)context.Node;

        if (localDeclaration.UsingKeyword.IsKind(SyntaxKind.UsingKeyword))
        {
            return;
        }

        foreach (var variable in localDeclaration.Declaration.Variables)
        {
            AnalyzeVariable(context, localDeclaration, variable);
        }
    }

    private static void AnalyzeVariable(
        SyntaxNodeAnalysisContext context,
        LocalDeclarationStatementSyntax localDeclaration,
        VariableDeclaratorSyntax variable)
    {
        if (context.SemanticModel.GetDeclaredSymbol(variable, context.CancellationToken)
            is not ILocalSymbol local)
        {
            return;
        }

        if (local.Type is not INamedTypeSymbol namedType)
        {
            return;
        }

        if (!TryGetTriedWrapperInfo(namedType, out var wrapperName, out var typeArgument))
        {
            return;
        }

        if (!ImplementsAnyDisposable(typeArgument!, out var disposableInterfaceName))
        {
            return;
        }

        var containingBody = FindEnclosingBody(localDeclaration);
        if (containingBody is null)
        {
            return;
        }

        if (IsOwnershipTransferredOrDisposed(local, containingBody, context))
        {
            return;
        }

        var diagnostic = Diagnostic.Create(
            DiagnosticDescriptors.TriedDisposableValueNotDisposed,
            variable.Identifier.GetLocation(),
            local.Name,
            wrapperName,
            typeArgument!.ToDisplayString(),
            disposableInterfaceName);
        context.ReportDiagnostic(diagnostic);
    }

    private static bool TryGetTriedWrapperInfo(
        INamedTypeSymbol namedType,
        out string wrapperName,
        out ITypeSymbol? typeArgument)
    {
        wrapperName = string.Empty;
        typeArgument = null;

        if (namedType.Name is not ("TriedEx" or "TriedNullEx" or "Tried"))
        {
            return false;
        }

        if (namedType.ContainingNamespace?.ToDisplayString() != "NexusLabs.Framework")
        {
            return false;
        }

        if (namedType.TypeArguments.Length != 1)
        {
            return false;
        }

        wrapperName = namedType.Name;
        typeArgument = namedType.TypeArguments[0];
        return true;
    }

    private static bool ImplementsAnyDisposable(
        ITypeSymbol typeArgument,
        out string disposableInterfaceName)
    {
        disposableInterfaceName = string.Empty;

        if (typeArgument is ITypeParameterSymbol)
        {
            return false;
        }

        if (typeArgument.SpecialType == SpecialType.System_Object)
        {
            return false;
        }

        var implementsAsync = false;
        var implementsSync = false;

        foreach (var iface in typeArgument.AllInterfaces)
        {
            var fullName = iface.ToDisplayString();
            if (fullName == "System.IAsyncDisposable")
            {
                implementsAsync = true;
            }
            else if (fullName == "System.IDisposable")
            {
                implementsSync = true;
            }
        }

        if (typeArgument.ToDisplayString() is "System.IDisposable")
        {
            implementsSync = true;
        }
        else if (typeArgument.ToDisplayString() is "System.IAsyncDisposable")
        {
            implementsAsync = true;
        }

        if (implementsAsync && implementsSync)
        {
            disposableInterfaceName = "IDisposable and IAsyncDisposable";
        }
        else if (implementsAsync)
        {
            disposableInterfaceName = "IAsyncDisposable";
        }
        else if (implementsSync)
        {
            disposableInterfaceName = "IDisposable";
        }

        return implementsAsync || implementsSync;
    }

    private static SyntaxNode? FindEnclosingBody(SyntaxNode node)
    {
        for (var current = node.Parent; current is not null; current = current.Parent)
        {
            switch (current)
            {
                case MethodDeclarationSyntax method when method.Body is not null:
                    return method.Body;
                case MethodDeclarationSyntax method when method.ExpressionBody is not null:
                    return method.ExpressionBody.Expression;
                case LocalFunctionStatementSyntax local when local.Body is not null:
                    return local.Body;
                case AccessorDeclarationSyntax accessor when accessor.Body is not null:
                    return accessor.Body;
                case ConstructorDeclarationSyntax ctor when ctor.Body is not null:
                    return ctor.Body;
                case AnonymousFunctionExpressionSyntax anon when anon.Body is not null:
                    return anon.Body;
            }
        }

        return null;
    }

    private static bool IsOwnershipTransferredOrDisposed(
        ILocalSymbol local,
        SyntaxNode body,
        SyntaxNodeAnalysisContext context)
    {
        var semanticModel = context.SemanticModel;
        var cancellationToken = context.CancellationToken;

        var identifierUses = body.DescendantNodes()
            .OfType<IdentifierNameSyntax>()
            .Where(id => id.Identifier.ValueText == local.Name);

        foreach (var identifier in identifierUses)
        {
            var symbol = semanticModel.GetSymbolInfo(identifier, cancellationToken).Symbol;
            if (!SymbolEqualityComparer.Default.Equals(symbol, local))
            {
                continue;
            }

            if (IsDisposeInvocation(identifier))
            {
                return true;
            }

            if (IsReturned(identifier))
            {
                return true;
            }

            if (IsPassedAsArgument(identifier))
            {
                return true;
            }

            if (IsAssignedToFieldOrProperty(identifier, semanticModel, cancellationToken))
            {
                return true;
            }

            if (IsInUsingStatement(identifier))
            {
                return true;
            }

            if (IsAwaitedAsAsyncDispose(identifier))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsDisposeInvocation(IdentifierNameSyntax identifier)
    {
        if (identifier.Parent is not MemberAccessExpressionSyntax memberAccess)
        {
            return false;
        }

        if (memberAccess.Expression != identifier)
        {
            return false;
        }

        if (memberAccess.Name.Identifier.ValueText is not ("Dispose" or "DisposeAsync"))
        {
            return false;
        }

        return memberAccess.Parent is InvocationExpressionSyntax;
    }

    private static bool IsReturned(IdentifierNameSyntax identifier)
    {
        for (var current = identifier.Parent; current is not null; current = current.Parent)
        {
            switch (current)
            {
                case ReturnStatementSyntax:
                case YieldStatementSyntax:
                case ArrowExpressionClauseSyntax:
                    return true;
                case StatementSyntax:
                    return false;
            }
        }

        return false;
    }

    private static bool IsPassedAsArgument(IdentifierNameSyntax identifier)
    {
        return identifier.Parent is ArgumentSyntax;
    }

    private static bool IsAssignedToFieldOrProperty(
        IdentifierNameSyntax identifier,
        SemanticModel semanticModel,
        System.Threading.CancellationToken cancellationToken)
    {
        if (identifier.Parent is not AssignmentExpressionSyntax assignment)
        {
            return false;
        }

        if (assignment.Right != identifier)
        {
            return false;
        }

        var target = semanticModel.GetSymbolInfo(assignment.Left, cancellationToken).Symbol;
        return target is IFieldSymbol or IPropertySymbol;
    }

    private static bool IsInUsingStatement(IdentifierNameSyntax identifier)
    {
        return identifier.Parent is UsingStatementSyntax usingStatement
            && usingStatement.Expression == identifier;
    }

    private static bool IsAwaitedAsAsyncDispose(IdentifierNameSyntax identifier)
    {
        if (identifier.Parent is not MemberAccessExpressionSyntax memberAccess)
        {
            return false;
        }

        if (memberAccess.Name.Identifier.ValueText != "DisposeAsync")
        {
            return false;
        }

        if (memberAccess.Parent is not InvocationExpressionSyntax invocation)
        {
            return false;
        }

        return invocation.Parent is AwaitExpressionSyntax;
    }
}
