using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NexusLabs.Framework.Analyzers;

/// <summary>
/// Per-compilation index of the members that a test framework invokes as a
/// data source — the target of attributes such as <c>[MemberData]</c>,
/// <c>[TestCaseSource]</c>, <c>[DynamicData]</c>, and <c>[MethodDataSource]</c>.
/// </summary>
/// <remarks>
/// The index is built from the whole compilation because the attribute lives on
/// the consuming test method, not on the data source itself. Construction is
/// deferred until the first lookup and memoized without caching failures, so a
/// cancelled build is retried rather than poisoning the instance.
/// </remarks>
internal sealed class TestDataSourceMemberIndex
{
    private const string AttributeSuffix = "Attribute";

    private static readonly ImmutableHashSet<string> DataSourceAttributeNames =
        ImmutableHashSet.Create(
            StringComparer.Ordinal,
            "MemberData",
            "TestCaseSource",
            "ValueSource",
            "DynamicData",
            "MethodDataSource");

    private readonly Compilation _compilation;

    private ImmutableHashSet<ISymbol>? _members;

    public TestDataSourceMemberIndex(Compilation compilation)
    {
        _compilation = compilation;
    }

    /// <summary>
    /// Determines whether <paramref name="member"/> is named by a test data
    /// source attribute anywhere in the compilation.
    /// </summary>
    /// <param name="member">The member to test.</param>
    /// <param name="cancellationToken">Cancels index construction.</param>
    /// <returns>
    /// <see langword="true"/> when a data source attribute in this compilation
    /// resolves to <paramref name="member"/>; otherwise <see langword="false"/>.
    /// A data source declared in another assembly is invisible here and returns
    /// <see langword="false"/>.
    /// </returns>
    public bool Contains(ISymbol member, CancellationToken cancellationToken)
    {
        var members = Volatile.Read(ref _members);
        if (members is null)
        {
            members = BuildIndex(_compilation, cancellationToken);
            Interlocked.CompareExchange(ref _members, members, comparand: null);
            members = Volatile.Read(ref _members)!;
        }

        return members.Contains(member.OriginalDefinition);
    }

    private static ImmutableHashSet<ISymbol> BuildIndex(
        Compilation compilation,
        CancellationToken cancellationToken)
    {
        var builder = ImmutableHashSet.CreateBuilder<ISymbol>(SymbolEqualityComparer.Default);

        foreach (var syntaxTree in compilation.SyntaxTrees)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var root = syntaxTree.GetRoot(cancellationToken);
            SemanticModel? semanticModel = null;

            foreach (var attribute in root.DescendantNodes(DescendIntoDeclarations).OfType<AttributeSyntax>())
            {
                if (!IsDataSourceAttribute(attribute))
                {
                    continue;
                }

                semanticModel ??= compilation.GetSemanticModel(syntaxTree);
                Collect(semanticModel, attribute, builder, cancellationToken);
            }
        }

        return builder.ToImmutable();
    }

    /// <summary>
    /// Skips method bodies, which hold most of a file's nodes. The only
    /// attribute they can carry belongs to a local function, and a local
    /// function can never be a data source — every framework resolves the
    /// member by reflecting over a type.
    /// </summary>
    private static bool DescendIntoDeclarations(SyntaxNode node)
    {
        return node is not StatementSyntax and not ExpressionSyntax;
    }

    private static void Collect(
        SemanticModel semanticModel,
        AttributeSyntax attribute,
        ImmutableHashSet<ISymbol>.Builder builder,
        CancellationToken cancellationToken)
    {
        var arguments = attribute.ArgumentList?.Arguments;
        if (arguments is null)
        {
            return;
        }

        string? memberName = null;
        var declaringType = GetAttributeTypeArgument(semanticModel, attribute.Name, cancellationToken);

        foreach (var argument in arguments.Value)
        {
            if (argument.Expression is TypeOfExpressionSyntax typeOfExpression)
            {
                declaringType ??= semanticModel
                    .GetTypeInfo(typeOfExpression.Type, cancellationToken)
                    .Type as INamedTypeSymbol;
                continue;
            }

            if (memberName is null && argument.NameEquals is null)
            {
                memberName = TryGetMemberName(argument.Expression);
            }
        }

        if (memberName is null)
        {
            return;
        }

        declaringType ??= GetEnclosingType(semanticModel, attribute, cancellationToken);

        for (var type = declaringType?.OriginalDefinition; type is not null; type = type.BaseType)
        {
            foreach (var member in type.GetMembers(memberName))
            {
                builder.Add(member.OriginalDefinition);
            }
        }
    }

    /// <summary>
    /// Reads the declaring type from a generic attribute such as
    /// <c>[MethodDataSource&lt;SharedCases&gt;(nameof(SharedCases.GetCasesAsync))]</c>,
    /// where the type is a type argument rather than a constructor argument.
    /// </summary>
    private static INamedTypeSymbol? GetAttributeTypeArgument(
        SemanticModel semanticModel,
        NameSyntax attributeName,
        CancellationToken cancellationToken)
    {
        if (GetSimpleName(attributeName) is not GenericNameSyntax genericName
            || genericName.TypeArgumentList.Arguments.Count != 1)
        {
            return null;
        }

        return semanticModel
            .GetTypeInfo(genericName.TypeArgumentList.Arguments[0], cancellationToken)
            .Type as INamedTypeSymbol;
    }

    /// <summary>
    /// Reads the member name from an attribute argument. Every supported
    /// attribute takes the name as its first non-<c>typeof</c> positional
    /// argument, either as a string literal or as <c>nameof(...)</c>.
    /// </summary>
    private static string? TryGetMemberName(ExpressionSyntax expression)
    {
        if (expression is LiteralExpressionSyntax literal
            && literal.Token.Value is string literalValue)
        {
            return literalValue;
        }

        if (expression is InvocationExpressionSyntax invocation
            && invocation.Expression is IdentifierNameSyntax identifier
            && identifier.Identifier.ValueText == "nameof"
            && invocation.ArgumentList.Arguments.Count == 1)
        {
            return GetRightmostIdentifier(invocation.ArgumentList.Arguments[0].Expression);
        }

        return null;
    }

    private static string? GetRightmostIdentifier(ExpressionSyntax expression)
    {
        return expression switch
        {
            SimpleNameSyntax simpleName => simpleName.Identifier.ValueText,
            MemberAccessExpressionSyntax memberAccess => GetRightmostIdentifier(memberAccess.Name),
            _ => null,
        };
    }

    private static INamedTypeSymbol? GetEnclosingType(
        SemanticModel semanticModel,
        AttributeSyntax attribute,
        CancellationToken cancellationToken)
    {
        var typeDeclaration = attribute.FirstAncestorOrSelf<TypeDeclarationSyntax>();
        return typeDeclaration is null
            ? null
            : semanticModel.GetDeclaredSymbol(typeDeclaration, cancellationToken);
    }

    private static bool IsDataSourceAttribute(AttributeSyntax attribute)
    {
        var simpleName = GetSimpleName(attribute.Name);
        if (simpleName is null)
        {
            return false;
        }

        var name = simpleName.Identifier.ValueText;
        if (name.EndsWith(AttributeSuffix, StringComparison.Ordinal))
        {
            name = name.Substring(0, name.Length - AttributeSuffix.Length);
        }

        return DataSourceAttributeNames.Contains(name);
    }

    private static SimpleNameSyntax? GetSimpleName(NameSyntax name)
    {
        return name switch
        {
            SimpleNameSyntax simpleName => simpleName,
            QualifiedNameSyntax qualifiedName => qualifiedName.Right,
            AliasQualifiedNameSyntax aliasQualifiedName => aliasQualifiedName.Name,
            _ => null,
        };
    }
}
