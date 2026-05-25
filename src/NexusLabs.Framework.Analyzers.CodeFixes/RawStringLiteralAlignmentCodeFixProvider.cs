using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace NexusLabs.Framework.Analyzers;

/// <summary>
/// Code fix for <see cref="DiagnosticDescriptors.RawStringOpeningQuotesMustBeOnOwnLine"/>
/// (NLF0010). Inserts a newline + indent before the opening triple-quote so it ends up
/// on its own line aligned with the closing triple-quote.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RawStringLiteralAlignmentCodeFixProvider)), Shared]
public sealed class RawStringLiteralAlignmentCodeFixProvider : CodeFixProvider
{
    private const string Title = "Move opening triple-quote to its own line";

    public override ImmutableArray<string> FixableDiagnosticIds =>
        // String literal (rather than DiagnosticDescriptors.X.Id) so this assembly
        // doesn't have to take a project reference on NexusLabs.Framework.Analyzers
        // for one constant. Tests fail loudly if the ID drifts.
        ImmutableArray.Create("NLF0010");

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return;
        }

        var diagnostic = context.Diagnostics[0];
        var span = diagnostic.Location.SourceSpan;
        var node = root.FindNode(span, getInnermostNodeForTie: true);

        var target = node.AncestorsAndSelf().FirstOrDefault(IsTargetLiteral);
        if (target is null)
        {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(
                title: Title,
                createChangedDocument: ct => MoveOpeningToOwnLineAsync(context.Document, target, ct),
                equivalenceKey: Title),
            diagnostic);
    }

    private static bool IsTargetLiteral(SyntaxNode node)
    {
        if (node is LiteralExpressionSyntax literal &&
            literal.Token.IsKind(SyntaxKind.MultiLineRawStringLiteralToken))
        {
            return true;
        }

        if (node is InterpolatedStringExpressionSyntax interp &&
            interp.StringStartToken.IsKind(SyntaxKind.InterpolatedMultiLineRawStringStartToken))
        {
            return true;
        }

        return false;
    }

    private static async Task<Document> MoveOpeningToOwnLineAsync(
        Document document,
        SyntaxNode target,
        CancellationToken cancellationToken)
    {
        var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

        if (!TryGetOpeningToken(target, out var openingToken))
        {
            return document;
        }

        // Walk back from the literal's full end through the trailing closing quotes
        // to find the position of the first character of the closing triple-quote.
        // We can't rely on InterpolatedStringExpressionSyntax.StringEndToken.SpanStart —
        // for multi-line interpolated raw strings, that token's SpanStart sits at the
        // end of the preceding content line, not at the closing quote run.
        var literalEnd = target.Span.End;
        var closingQuoteCount = 0;
        while (literalEnd - closingQuoteCount - 1 >= 0 &&
               sourceText[literalEnd - closingQuoteCount - 1] == '"')
        {
            closingQuoteCount++;
        }

        if (closingQuoteCount == 0)
        {
            return document;
        }

        var closingStart = literalEnd - closingQuoteCount;
        var closingLine = sourceText.Lines.GetLineFromPosition(closingStart);
        var leadingWhitespace = sourceText.ToString(new TextSpan(closingLine.Start, closingStart - closingLine.Start));
        var newline = DetectNewline(sourceText, closingLine.Start);

        // Compute the span of horizontal whitespace immediately preceding the
        // opening triple-quote on the same line, then replace it with
        // newline + matching-indent so the opening lands on its own line.
        var openingStart = openingToken.SpanStart;
        var trimStart = openingStart;
        while (trimStart > 0)
        {
            var c = sourceText[trimStart - 1];
            if (c != ' ' && c != '\t')
            {
                break;
            }
            trimStart--;
        }

        var newText = sourceText.Replace(
            new TextSpan(trimStart, openingStart - trimStart),
            newline + leadingWhitespace);

        return document.WithText(newText);
    }

    private static bool TryGetOpeningToken(SyntaxNode target, out SyntaxToken openingToken)
    {
        if (target is LiteralExpressionSyntax literal &&
            literal.Token.IsKind(SyntaxKind.MultiLineRawStringLiteralToken))
        {
            openingToken = literal.Token;
            return true;
        }

        if (target is InterpolatedStringExpressionSyntax interp &&
            interp.StringStartToken.IsKind(SyntaxKind.InterpolatedMultiLineRawStringStartToken))
        {
            openingToken = interp.StringStartToken;
            return true;
        }

        openingToken = default;
        return false;
    }

    private static string DetectNewline(SourceText sourceText, int closingLineStart)
    {
        if (closingLineStart >= 2 &&
            sourceText[closingLineStart - 2] == '\r' &&
            sourceText[closingLineStart - 1] == '\n')
        {
            return "\r\n";
        }

        return "\n";
    }
}
