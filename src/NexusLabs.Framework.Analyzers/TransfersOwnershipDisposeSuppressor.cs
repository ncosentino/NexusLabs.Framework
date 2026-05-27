using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace NexusLabs.Framework.Analyzers;

/// <summary>
/// Suppresses IDISP007 ("Don't dispose injected") when the dispose target
/// or the boolean guard that gates the dispose call is annotated with
/// <c>[NexusLabs.Framework.TransfersOwnership]</c>.
/// </summary>
/// <remarks>
/// <para>
/// Two supported shapes — both require <c>[TransfersOwnership]</c> on a
/// field, property, or parameter of the declaring type. The suppressor uses
/// the semantic model to resolve symbols; identifier names are irrelevant
/// outside of the explicit <c>Targets</c> list described below.
/// </para>
/// <para>
/// <strong>Shape B (direct):</strong> the dispose target itself carries
/// the attribute (parameterless). Any <c>field.Dispose()</c> /
/// <c>field.DisposeAsync()</c> (or qualified <c>this.field.Dispose()</c>)
/// where <c>field</c> resolves to a member annotated with
/// <c>[TransfersOwnership]</c> is suppressed. Wrapping idioms are
/// recognised too — for example
/// <c>await _field.DisposeAsync().ConfigureAwait(false)</c>, where the
/// underlying analyzer may anchor the diagnostic on the surrounding
/// <c>await</c> keyword rather than the inner invocation. The
/// <c>Targets</c> list is ignored on Shape B.
/// </para>
/// <para>
/// <strong>Shape A (conditional, strict targets):</strong> the dispose
/// call sits inside an <c>if</c>-statement whose condition requires an
/// annotated boolean member to be true, AND the dispose receiver's simple
/// name appears in the annotation's <c>Targets</c> list. Recognised
/// condition shapes: <c>if (flag)</c>, <c>if (this.flag)</c>, parenthesised
/// forms, and any logical-AND chain that includes such a member.
/// Disjunctions (<c>||</c>) are not honoured.
/// </para>
/// <para>
/// A flag with an empty <c>Targets</c> list never suppresses — this is
/// deliberate. The old "any dispose inside a guarded body is suppressed"
/// behaviour silenced legitimate IDISP007 hits on disposables the guard
/// did NOT actually own (e.g., two disposables disposed inside the same
/// if-block, but only one transferred). Strict targeting fixes that.
/// </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TransfersOwnershipDisposeSuppressor : DiagnosticSuppressor
{
    private const string SuppressedDiagnosticId = "IDISP007";
    private const string AttributeName = "TransfersOwnershipAttribute";
    private const string AttributeNamespace = "NexusLabs.Framework";

    private static readonly SuppressionDescriptor _rule = new(
        id: "NLFSUP001",
        suppressedDiagnosticId: SuppressedDiagnosticId,
        justification:
            "Dispose target (or the boolean guard around the dispose call) is " +
            "annotated with [NexusLabs.Framework.TransfersOwnership], which " +
            "declares that ownership of the disposable was intentionally " +
            "transferred to the declaring type. Recognised shapes: " +
            "(1) field/property carrying [TransfersOwnership] disposed " +
            "directly; (2) dispose call inside if (<bool>) where the " +
            "boolean member carries [TransfersOwnership(nameof(<field>))] " +
            "and the dispose receiver matches one of the listed targets. " +
            "Disjunctions (||) and empty target lists are deliberately " +
            "not honoured.");

    public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions =>
        ImmutableArray.Create(_rule);

    public override void ReportSuppressions(SuppressionAnalysisContext context)
    {
        foreach (var diagnostic in context.ReportedDiagnostics)
        {
            if (!string.Equals(diagnostic.Id, SuppressedDiagnosticId, StringComparison.Ordinal))
            {
                continue;
            }

            if (ShouldSuppress(diagnostic, context))
            {
                context.ReportSuppression(Suppression.Create(_rule, diagnostic));
            }
        }
    }

    private static bool ShouldSuppress(
        Diagnostic diagnostic,
        SuppressionAnalysisContext context)
    {
        var location = diagnostic.Location;
        var tree = location.SourceTree;
        if (tree is null)
        {
            return false;
        }

        var cancellationToken = context.CancellationToken;
        var root = tree.GetRoot(cancellationToken);
        var node = root.FindNode(location.SourceSpan, getInnermostNodeForTie: true);
        var semanticModel = context.GetSemanticModel(tree);

        if (DisposeTargetHasAttribute(node, semanticModel, cancellationToken))
        {
            return true;
        }

        return EnclosingIfConditionAuthorisesDispose(
            node,
            location.SourceSpan,
            semanticModel,
            cancellationToken);
    }

    private static bool DisposeTargetHasAttribute(
        SyntaxNode node,
        SemanticModel semanticModel,
        System.Threading.CancellationToken cancellationToken)
    {
        var ancestor = node.FirstAncestorOrSelf<InvocationExpressionSyntax>();
        if (ancestor is not null &&
            IsDisposeCallOnAnnotatedTarget(ancestor, semanticModel, cancellationToken))
        {
            return true;
        }

        foreach (var descendant in node.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (IsDisposeCallOnAnnotatedTarget(descendant, semanticModel, cancellationToken))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsDisposeCallOnAnnotatedTarget(
        InvocationExpressionSyntax invocation,
        SemanticModel semanticModel,
        System.Threading.CancellationToken cancellationToken)
    {
        if (invocation.Expression is not MemberAccessExpressionSyntax member)
        {
            return false;
        }

        if (member.Name.Identifier.ValueText is not ("Dispose" or "DisposeAsync"))
        {
            return false;
        }

        var receiver = member.Expression;
        var receiverSymbol = semanticModel
            .GetSymbolInfo(receiver, cancellationToken)
            .Symbol;
        return receiverSymbol is not null && HasTransfersOwnership(receiverSymbol);
    }

    private static bool EnclosingIfConditionAuthorisesDispose(
        SyntaxNode node,
        TextSpan location,
        SemanticModel semanticModel,
        System.Threading.CancellationToken cancellationToken)
    {
        for (SyntaxNode? current = node; current is not null; current = current.Parent)
        {
            if (current is not IfStatementSyntax ifStatement)
            {
                continue;
            }

            if (!ifStatement.Statement.Span.Contains(location))
            {
                continue;
            }

            var allowedTargets = CollectAllowedTargetsFromCondition(
                ifStatement.Condition,
                semanticModel,
                cancellationToken);

            if (allowedTargets is null || allowedTargets.Count == 0)
            {
                continue;
            }

            var receiverName = TryGetDisposeReceiverName(
                node,
                semanticModel,
                cancellationToken);

            if (receiverName is null)
            {
                continue;
            }

            if (allowedTargets.Contains(receiverName))
            {
                return true;
            }
        }

        return false;
    }

    private static HashSet<string>? CollectAllowedTargetsFromCondition(
        ExpressionSyntax? condition,
        SemanticModel semanticModel,
        System.Threading.CancellationToken cancellationToken)
    {
        if (condition is null)
        {
            return null;
        }

        var accumulator = new HashSet<string>(StringComparer.Ordinal);
        var foundAnnotated = false;

        CollectAllowedTargetsCore(
            condition,
            semanticModel,
            cancellationToken,
            accumulator,
            ref foundAnnotated);

        return foundAnnotated ? accumulator : null;
    }

    private static void CollectAllowedTargetsCore(
        ExpressionSyntax condition,
        SemanticModel semanticModel,
        System.Threading.CancellationToken cancellationToken,
        HashSet<string> accumulator,
        ref bool foundAnnotated)
    {
        switch (condition)
        {
            case ParenthesizedExpressionSyntax paren:
                CollectAllowedTargetsCore(
                    paren.Expression,
                    semanticModel,
                    cancellationToken,
                    accumulator,
                    ref foundAnnotated);
                return;

            case BinaryExpressionSyntax binary when binary.IsKind(SyntaxKind.LogicalAndExpression):
                CollectAllowedTargetsCore(
                    binary.Left,
                    semanticModel,
                    cancellationToken,
                    accumulator,
                    ref foundAnnotated);
                CollectAllowedTargetsCore(
                    binary.Right,
                    semanticModel,
                    cancellationToken,
                    accumulator,
                    ref foundAnnotated);
                return;

            case IdentifierNameSyntax:
            case MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax }:
                var symbol = semanticModel
                    .GetSymbolInfo(condition, cancellationToken)
                    .Symbol;
                if (symbol is null)
                {
                    return;
                }

                var targets = TryGetTransfersOwnershipTargets(symbol);
                if (targets is null)
                {
                    return;
                }

                foundAnnotated = true;
                foreach (var target in targets)
                {
                    accumulator.Add(target);
                }

                return;

            default:
                return;
        }
    }

    private static string? TryGetDisposeReceiverName(
        SyntaxNode node,
        SemanticModel semanticModel,
        System.Threading.CancellationToken cancellationToken)
    {
        var ancestor = node.FirstAncestorOrSelf<InvocationExpressionSyntax>();
        if (ancestor is not null &&
            TryResolveDisposeReceiverName(
                ancestor,
                semanticModel,
                cancellationToken,
                out var ancestorName))
        {
            return ancestorName;
        }

        foreach (var descendant in node.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (TryResolveDisposeReceiverName(
                    descendant,
                    semanticModel,
                    cancellationToken,
                    out var descendantName))
            {
                return descendantName;
            }
        }

        return null;
    }

    private static bool TryResolveDisposeReceiverName(
        InvocationExpressionSyntax invocation,
        SemanticModel semanticModel,
        System.Threading.CancellationToken cancellationToken,
        out string? name)
    {
        name = null;

        if (invocation.Expression is not MemberAccessExpressionSyntax member)
        {
            return false;
        }

        if (member.Name.Identifier.ValueText is not ("Dispose" or "DisposeAsync"))
        {
            return false;
        }

        var receiverSymbol = semanticModel
            .GetSymbolInfo(member.Expression, cancellationToken)
            .Symbol;

        if (receiverSymbol is null)
        {
            return false;
        }

        name = receiverSymbol.Name;
        return true;
    }

    private static bool HasTransfersOwnership(ISymbol symbol)
    {
        foreach (var attribute in symbol.GetAttributes())
        {
            if (IsTransfersOwnershipAttribute(attribute))
            {
                return true;
            }
        }

        return false;
    }

    private static IReadOnlyList<string>? TryGetTransfersOwnershipTargets(ISymbol symbol)
    {
        foreach (var attribute in symbol.GetAttributes())
        {
            if (!IsTransfersOwnershipAttribute(attribute))
            {
                continue;
            }

            return ExtractTargets(attribute);
        }

        return null;
    }

    private static IReadOnlyList<string> ExtractTargets(AttributeData attribute)
    {
        if (attribute.ConstructorArguments.Length == 0)
        {
            return Array.Empty<string>();
        }

        var arg = attribute.ConstructorArguments[0];
        if (arg.Kind != TypedConstantKind.Array || arg.Values.IsDefaultOrEmpty)
        {
            return Array.Empty<string>();
        }

        var builder = new List<string>(arg.Values.Length);
        foreach (var value in arg.Values)
        {
            if (value.Value is string name)
            {
                builder.Add(name);
            }
        }

        return builder;
    }

    private static bool IsTransfersOwnershipAttribute(AttributeData attribute)
    {
        var attributeClass = attribute.AttributeClass;
        if (attributeClass is null)
        {
            return false;
        }

        return string.Equals(attributeClass.Name, AttributeName, StringComparison.Ordinal) &&
               IsInNexusLabsFrameworkNamespace(attributeClass.ContainingNamespace);
    }

    private static bool IsInNexusLabsFrameworkNamespace(INamespaceSymbol? ns)
    {
        if (ns is null || ns.IsGlobalNamespace)
        {
            return false;
        }

        return string.Equals(
            ns.ToDisplayString(),
            AttributeNamespace,
            StringComparison.Ordinal);
    }
}
