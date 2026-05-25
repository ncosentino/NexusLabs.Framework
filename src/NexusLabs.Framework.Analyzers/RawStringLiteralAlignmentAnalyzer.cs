using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace NexusLabs.Framework.Analyzers;

/// <summary>
/// Flags multi-line raw string literals (<c>"""..."""</c> and <c>$"""..."""</c>) whose
/// opening <c>"""</c> token sits on the same line as preceding non-whitespace code,
/// dangling after an assignment, return, or argument. Convention requires the opening
/// token to be on its own line so that the opening, content, and closing
/// <c>"""</c> share a single visual column.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RawStringLiteralAlignmentAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.RawStringOpeningQuotesMustBeOnOwnLine);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeStringLiteral, SyntaxKind.StringLiteralExpression);
        context.RegisterSyntaxNodeAction(AnalyzeInterpolatedString, SyntaxKind.InterpolatedStringExpression);
    }

    private static void AnalyzeStringLiteral(SyntaxNodeAnalysisContext context)
    {
        var literal = (LiteralExpressionSyntax)context.Node;
        var token = literal.Token;

        if (!token.IsKind(SyntaxKind.MultiLineRawStringLiteralToken))
        {
            return;
        }

        ReportIfDangling(context, token);
    }

    private static void AnalyzeInterpolatedString(SyntaxNodeAnalysisContext context)
    {
        var interp = (InterpolatedStringExpressionSyntax)context.Node;
        var startToken = interp.StringStartToken;

        if (!startToken.IsKind(SyntaxKind.InterpolatedMultiLineRawStringStartToken))
        {
            return;
        }

        ReportIfDangling(context, startToken);
    }

    private static void ReportIfDangling(
        SyntaxNodeAnalysisContext context,
        SyntaxToken openingToken)
    {
        var syntaxTree = openingToken.SyntaxTree;
        if (syntaxTree is null)
        {
            return;
        }

        var sourceText = syntaxTree.GetText(context.CancellationToken);
        var openingLine = sourceText.Lines.GetLineFromPosition(openingToken.SpanStart);
        var startColumn = openingToken.SpanStart - openingLine.Start;

        if (startColumn <= 0)
        {
            return;
        }

        var precedingSpan = new TextSpan(openingLine.Start, startColumn);
        var precedingText = sourceText.ToString(precedingSpan);

        if (string.IsNullOrWhiteSpace(precedingText))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.RawStringOpeningQuotesMustBeOnOwnLine,
            OpeningSyntaxLocation(openingToken, syntaxTree)));
    }

    private static Location OpeningSyntaxLocation(SyntaxToken token, SyntaxTree syntaxTree)
    {
        var text = token.Text;
        var dollarCount = 0;
        while (dollarCount < text.Length && text[dollarCount] == '$')
        {
            dollarCount++;
        }

        var quoteCount = 0;
        while (dollarCount + quoteCount < text.Length && text[dollarCount + quoteCount] == '"')
        {
            quoteCount++;
        }

        if (quoteCount == 0)
        {
            return token.GetLocation();
        }

        return Location.Create(syntaxTree, new TextSpan(token.SpanStart, dollarCount + quoteCount));
    }
}
