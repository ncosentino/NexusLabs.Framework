using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Simplification;

namespace NexusLabs.Framework.Analyzers;

/// <summary>
/// Code fix for <see cref="DiagnosticDescriptors.DoNotAllocateEmptyReadOnlyCollection"/>
/// (NLF0019). Replaces the empty mutable-collection creation with the shared empty
/// instance appropriate to the read-only interface it is widened to: <c>[]</c> for the
/// list family, <c>ReadOnlyDictionary&lt;TKey,TValue&gt;.Empty</c> for read-only
/// dictionaries, and <c>FrozenSet&lt;T&gt;.Empty</c> for read-only sets.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(EmptyReadOnlyCollectionCodeFixProvider)), Shared]
public sealed class EmptyReadOnlyCollectionCodeFixProvider : CodeFixProvider
{
    private const string Title = "Use the shared empty collection";

    public override ImmutableArray<string> FixableDiagnosticIds =>
        // String literal (rather than DiagnosticDescriptors.X.Id) so this assembly
        // doesn't have to take a project reference on NexusLabs.Framework.Analyzers
        // for one constant. Tests fail loudly if the ID drifts.
        ImmutableArray.Create("NLF0019");

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return;
        }

        var diagnostic = context.Diagnostics[0];
        var node = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
        var creation = node.FirstAncestorOrSelf<ObjectCreationExpressionSyntax>();
        if (creation is null)
        {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(
                title: Title,
                createChangedDocument: ct => ReplaceAsync(context.Document, creation, ct),
                equivalenceKey: Title),
            diagnostic);
    }

    private static async Task<Document> ReplaceAsync(
        Document document,
        ObjectCreationExpressionSyntax creation,
        CancellationToken cancellationToken)
    {
        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (semanticModel is null)
        {
            return document;
        }

        if (semanticModel.GetTypeInfo(creation, cancellationToken).ConvertedType is not INamedTypeSymbol convertedType)
        {
            return document;
        }

        var generator = SyntaxGenerator.GetGenerator(document);
        var replacement = BuildReplacement(convertedType, semanticModel.Compilation, generator);
        if (replacement is null)
        {
            return document;
        }

        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return document;
        }

        var newRoot = root.ReplaceNode(creation, replacement.WithTriviaFrom(creation));
        return document.WithSyntaxRoot(newRoot);
    }

    private static SyntaxNode? BuildReplacement(
        INamedTypeSymbol convertedType,
        Compilation compilation,
        SyntaxGenerator generator)
    {
        switch (convertedType.OriginalDefinition.ToDisplayString())
        {
            case "System.Collections.Generic.IEnumerable<T>":
            case "System.Collections.Generic.IReadOnlyCollection<T>":
            case "System.Collections.Generic.IReadOnlyList<T>":
                return SyntaxFactory.CollectionExpression();

            case "System.Collections.Generic.IReadOnlyDictionary<TKey, TValue>":
                return EmptyMember(compilation, generator, "System.Collections.ObjectModel.ReadOnlyDictionary`2", convertedType.TypeArguments);

            case "System.Collections.Generic.IReadOnlySet<T>":
                return EmptyMember(compilation, generator, "System.Collections.Frozen.FrozenSet`1", convertedType.TypeArguments);

            default:
                return null;
        }
    }

    private static SyntaxNode? EmptyMember(
        Compilation compilation,
        SyntaxGenerator generator,
        string metadataName,
        ImmutableArray<ITypeSymbol> typeArguments)
    {
        var definition = compilation.GetTypeByMetadataName(metadataName);
        if (definition is null)
        {
            return null;
        }

        var constructed = definition.Construct(typeArguments.ToArray());
        var typeExpression = generator.TypeExpression(constructed);
        var memberAccess = generator.MemberAccessExpression(typeExpression, "Empty");

        return memberAccess.WithAdditionalAnnotations(Simplifier.Annotation, Simplifier.AddImportsAnnotation);
    }
}
