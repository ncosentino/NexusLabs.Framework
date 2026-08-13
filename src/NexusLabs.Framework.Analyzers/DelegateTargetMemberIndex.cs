using System.Collections.Immutable;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace NexusLabs.Framework.Analyzers;

/// <summary>
/// Per-compilation index of the methods that are converted to a delegate
/// somewhere in the compilation — assigned to a delegate-typed member, passed
/// as a delegate argument, or subscribed to an event.
/// </summary>
/// <remarks>
/// A method group conversion binds the method to a signature the delegate type
/// owns. The index is built from the whole compilation because the conversion
/// is usually far from the declaration. Construction is deferred until the
/// first lookup and memoized without caching failures, so a cancelled build is
/// retried rather than poisoning the instance.
/// </remarks>
internal sealed class DelegateTargetMemberIndex
{
    private readonly Compilation _compilation;

    private ImmutableHashSet<ISymbol>? _members;

    public DelegateTargetMemberIndex(Compilation compilation)
    {
        _compilation = compilation;
    }

    /// <summary>
    /// Determines whether <paramref name="method"/> is converted to a delegate
    /// somewhere in the compilation.
    /// </summary>
    /// <param name="method">The method to test.</param>
    /// <param name="cancellationToken">Cancels index construction.</param>
    /// <returns>
    /// <see langword="true"/> when a conversion in this compilation binds
    /// <paramref name="method"/> to a delegate; otherwise
    /// <see langword="false"/>. A conversion performed in another assembly is
    /// invisible here and returns <see langword="false"/>.
    /// </returns>
    public bool Contains(IMethodSymbol method, CancellationToken cancellationToken)
    {
        var members = Volatile.Read(ref _members);
        if (members is null)
        {
            members = BuildIndex(_compilation, cancellationToken);
            Interlocked.CompareExchange(ref _members, members, comparand: null);
            members = Volatile.Read(ref _members)!;
        }

        return members.Contains(method.OriginalDefinition);
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

            foreach (var node in root.DescendantNodes())
            {
                if (!IsCandidateConversion(node))
                {
                    continue;
                }

                semanticModel ??= compilation.GetSemanticModel(syntaxTree);
                Collect(semanticModel, node, builder, cancellationToken);
            }
        }

        return builder.ToImmutable();
    }

    /// <summary>
    /// Cheap syntactic filter that keeps the semantic model off every
    /// identifier in the compilation. A method group conversion is a bare name
    /// — never the callee of an invocation, which always carries an argument
    /// list.
    /// </summary>
    private static bool IsCandidateConversion(SyntaxNode node)
    {
        if (node is not SimpleNameSyntax and not MemberAccessExpressionSyntax)
        {
            return false;
        }

        return node.Parent switch
        {
            InvocationExpressionSyntax invocation => invocation.Expression != node,
            MemberAccessExpressionSyntax memberAccess => memberAccess.Expression == node,
            _ => true,
        };
    }

    private static void Collect(
        SemanticModel semanticModel,
        SyntaxNode node,
        ImmutableHashSet<ISymbol>.Builder builder,
        CancellationToken cancellationToken)
    {
        if (semanticModel.GetOperation(node, cancellationToken) is not IMethodReferenceOperation reference)
        {
            return;
        }

        if (reference.Parent is not IDelegateCreationOperation)
        {
            return;
        }

        builder.Add(reference.Method.OriginalDefinition);
    }
}
