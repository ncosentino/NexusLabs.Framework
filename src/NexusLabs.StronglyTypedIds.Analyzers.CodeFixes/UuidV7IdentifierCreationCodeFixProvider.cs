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

namespace NexusLabs.StronglyTypedIds.Analyzers;

/// <summary>
/// Replaces UUIDv4-producing identifier creation with the generated UUIDv7
/// <c>Create()</c> API.
/// </summary>
[ExportCodeFixProvider(
    LanguageNames.CSharp,
    Name = nameof(UuidV7IdentifierCreationCodeFixProvider)),
 Shared]
public sealed class UuidV7IdentifierCreationCodeFixProvider : CodeFixProvider
{
    private const string Title = "Use the UUIDv7 Create() method";

    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create("NLS0001", "NLS0002");

    public override FixAllProvider GetFixAllProvider() =>
        WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document
            .GetSyntaxRootAsync(context.CancellationToken)
            .ConfigureAwait(false);
        if (root is null)
        {
            return;
        }

        var diagnostic = context.Diagnostics[0];
        var node = root.FindNode(
            diagnostic.Location.SourceSpan,
            getInnermostNodeForTie: true);

        switch (diagnostic.Id)
        {
            case "NLS0001":
                RegisterNewMethodFix(context, diagnostic, node);
                break;

            case "NLS0002":
                RegisterConstructionFix(context, diagnostic, node);
                break;
        }
    }

    private static void RegisterNewMethodFix(
        CodeFixContext context,
        Diagnostic diagnostic,
        SyntaxNode node)
    {
        var simpleName = node as SimpleNameSyntax ??
            node.FirstAncestorOrSelf<SimpleNameSyntax>();
        if (simpleName is null)
        {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(
                title: Title,
                createChangedDocument: cancellationToken =>
                    ReplaceNewMethodAsync(
                        context.Document,
                        simpleName,
                        cancellationToken),
                equivalenceKey: Title),
            diagnostic);
    }

    private static void RegisterConstructionFix(
        CodeFixContext context,
        Diagnostic diagnostic,
        SyntaxNode node)
    {
        var creation = node.FirstAncestorOrSelf<BaseObjectCreationExpressionSyntax>();
        if (creation is null)
        {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(
                title: Title,
                createChangedDocument: cancellationToken =>
                    ReplaceConstructionAsync(
                        context.Document,
                        creation,
                        cancellationToken),
                equivalenceKey: Title),
            diagnostic);
    }

    private static async Task<Document> ReplaceNewMethodAsync(
        Document document,
        SimpleNameSyntax simpleName,
        CancellationToken cancellationToken)
    {
        var root = await document
            .GetSyntaxRootAsync(cancellationToken)
            .ConfigureAwait(false);
        if (root is null)
        {
            return document;
        }

        var replacement = SyntaxFactory
            .IdentifierName("Create")
            .WithTriviaFrom(simpleName);
        return document.WithSyntaxRoot(root.ReplaceNode(simpleName, replacement));
    }

    private static async Task<Document> ReplaceConstructionAsync(
        Document document,
        BaseObjectCreationExpressionSyntax creation,
        CancellationToken cancellationToken)
    {
        var semanticModel = await document
            .GetSemanticModelAsync(cancellationToken)
            .ConfigureAwait(false);
        if (semanticModel?.GetTypeInfo(creation, cancellationToken).Type is not
            INamedTypeSymbol identifierType)
        {
            return document;
        }

        var root = await document
            .GetSyntaxRootAsync(cancellationToken)
            .ConfigureAwait(false);
        if (root is null)
        {
            return document;
        }

        var generator = SyntaxGenerator.GetGenerator(document);
        var replacement = generator.InvocationExpression(
                generator.MemberAccessExpression(
                    generator.TypeExpression(identifierType),
                    "Create"))
            .WithTriviaFrom(creation)
            .WithAdditionalAnnotations(
                Simplifier.Annotation,
                Simplifier.AddImportsAnnotation);

        return document.WithSyntaxRoot(root.ReplaceNode(creation, replacement));
    }
}
