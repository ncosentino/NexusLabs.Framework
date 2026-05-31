using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace NexusLabs.Framework.Analyzers;

/// <summary>
/// Flags the awkward "parse via backing type, then construct the ID" pattern
/// for types decorated with
/// <c>[StronglyTypedIds.StronglyTypedIdAttribute]</c> (NLF0013).
/// </summary>
/// <remarks>
/// <para>
/// Two shapes are detected, both gated on the constructed type having the
/// <c>[StronglyTypedId]</c> attribute AND exposing its own matching static
/// parser:
/// </para>
/// <list type="bullet">
///   <item>
///     <description>
///     <strong>Parse:</strong> <c>new XxxId(BackingType.Parse(s))</c>.
///     The fix is the equivalent <c>XxxId.Parse(s)</c>.
///     </description>
///   </item>
///   <item>
///     <description>
///     <strong>TryParse:</strong>
///     <c>if (BackingType.TryParse(s, out var g)) { var id = new XxxId(g); }</c>.
///     The fix is the equivalent
///     <c>if (XxxId.TryParse(s, out var id)) { ... }</c>.
///     </description>
///   </item>
/// </list>
/// <para>
/// The TryParse arm is intentionally conservative: it only fires when (a)
/// the <c>TryParse</c> invocation is the entire condition of an enclosing
/// <c>if</c> (no negation, no compound boolean), (b) the construction lives
/// inside that <c>if</c>'s success branch, (c) the out local is never
/// written to between its declaration and the construction site (using
/// <c>SemanticModel.AnalyzeDataFlow</c>), and (d) no lambda or local
/// function boundary separates the construction from the <c>if</c>. This
/// avoids changing semantics for cases like default-on-failure usage,
/// post-parse normalisation, or captured-in-lambda flow.
/// </para>
/// <para>
/// The Parse arm only matches the exact single-string overload
/// (<c>BackingType.Parse(string)</c>) and requires the target ID to expose
/// <c>static T Parse(string)</c>. The TryParse arm only matches the exact
/// <c>BackingType.TryParse(string, out BackingType)</c> shape and requires
/// the target ID to expose <c>static bool TryParse(string, out T)</c>.
/// Overloads taking <c>IFormatProvider</c>, <c>NumberStyles</c>, etc. are
/// left alone in this version — equivalent overloads on the target ID
/// would have to be verified individually.
/// </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class StronglyTypedIdParsePatternAnalyzer : DiagnosticAnalyzer
{
    private const string AttributeMetadataName = "StronglyTypedIds.StronglyTypedIdAttribute";

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.StronglyTypedIdParsePatternMisuse);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context)
    {
        var attributeType = context.Compilation.GetTypeByMetadataName(AttributeMetadataName);
        if (attributeType is null)
        {
            return;
        }

        var stringType = context.Compilation.GetSpecialType(SpecialType.System_String);
        var boolType = context.Compilation.GetSpecialType(SpecialType.System_Boolean);

        var types = new AnalyzerTypeContext(attributeType, stringType, boolType);

        context.RegisterSyntaxNodeAction(
            ctx => AnalyzeObjectCreation(ctx, types),
            SyntaxKind.ObjectCreationExpression,
            SyntaxKind.ImplicitObjectCreationExpression);
    }

    private static void AnalyzeObjectCreation(
        SyntaxNodeAnalysisContext context,
        AnalyzerTypeContext types)
    {
        var creation = (BaseObjectCreationExpressionSyntax)context.Node;

        if (creation.ArgumentList is null ||
            creation.ArgumentList.Arguments.Count != 1)
        {
            return;
        }

        var argument = creation.ArgumentList.Arguments[0];

        // The `new XxxId(out something)` shape never makes sense for this
        // pattern and would also collide with the TryParse out-arg detection.
        if (!argument.RefKindKeyword.IsKind(SyntaxKind.None))
        {
            return;
        }

        if (context.SemanticModel.GetSymbolInfo(creation, context.CancellationToken).Symbol
            is not IMethodSymbol ctor)
        {
            return;
        }

        if (ctor.MethodKind != MethodKind.Constructor)
        {
            return;
        }

        var idType = ctor.ContainingType;
        if (idType is null || !HasStronglyTypedIdAttribute(idType, types.AttributeType))
        {
            return;
        }

        if (ctor.Parameters.Length != 1)
        {
            return;
        }

        var backingType = ctor.Parameters[0].Type;
        if (backingType is null)
        {
            return;
        }

        var argumentExpression = StripParentheses(argument.Expression);

        if (TryReportParsePattern(
                context,
                creation,
                argumentExpression,
                backingType,
                idType,
                types))
        {
            return;
        }

        TryReportTryParsePattern(
            context,
            creation,
            argumentExpression,
            backingType,
            idType,
            types);
    }

    private static bool TryReportParsePattern(
        SyntaxNodeAnalysisContext context,
        BaseObjectCreationExpressionSyntax creation,
        ExpressionSyntax argumentExpression,
        ITypeSymbol backingType,
        INamedTypeSymbol idType,
        AnalyzerTypeContext types)
    {
        if (argumentExpression is not InvocationExpressionSyntax invocation)
        {
            return false;
        }

        if (context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol
            is not IMethodSymbol parseMethod)
        {
            return false;
        }

        if (!IsExactParseOnBackingType(parseMethod, backingType, types))
        {
            return false;
        }

        if (!IdTypeExposesParse(idType, types))
        {
            return false;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.StronglyTypedIdParsePatternMisuse,
            creation.GetLocation(),
            idType.Name,
            "Parse",
            backingType.ToDisplayString()));

        return true;
    }

    private static void TryReportTryParsePattern(
        SyntaxNodeAnalysisContext context,
        BaseObjectCreationExpressionSyntax creation,
        ExpressionSyntax argumentExpression,
        ITypeSymbol backingType,
        INamedTypeSymbol idType,
        AnalyzerTypeContext types)
    {
        if (argumentExpression is not IdentifierNameSyntax identifier)
        {
            return;
        }

        if (context.SemanticModel.GetSymbolInfo(identifier, context.CancellationToken).Symbol
            is not ILocalSymbol localSymbol)
        {
            return;
        }

        if (!TryGetOutVarTryParseInvocation(
                localSymbol,
                context.CancellationToken,
                out var tryParseInvocation))
        {
            return;
        }

        if (context.SemanticModel.GetSymbolInfo(tryParseInvocation, context.CancellationToken).Symbol
            is not IMethodSymbol tryParseMethod)
        {
            return;
        }

        if (!IsExactTryParseOnBackingType(tryParseMethod, backingType, types))
        {
            return;
        }

        if (!IdTypeExposesTryParse(idType, types))
        {
            return;
        }

        if (!IsInsideSuccessBranchOf(tryParseInvocation, creation, out var ifThenBranch))
        {
            return;
        }

        if (CrossesLambdaOrLocalFunctionBoundary(creation, ifThenBranch))
        {
            return;
        }

        if (LocalIsWrittenInsideBranch(context.SemanticModel, ifThenBranch, localSymbol))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.StronglyTypedIdParsePatternMisuse,
            creation.GetLocation(),
            idType.Name,
            "TryParse",
            backingType.ToDisplayString()));
    }

    private static bool HasStronglyTypedIdAttribute(
        ITypeSymbol type,
        INamedTypeSymbol attributeType)
    {
        foreach (var attribute in type.GetAttributes())
        {
            if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, attributeType))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsExactParseOnBackingType(
        IMethodSymbol parseMethod,
        ITypeSymbol backingType,
        AnalyzerTypeContext types)
    {
        if (!parseMethod.IsStatic ||
            parseMethod.Name != "Parse" ||
            parseMethod.Parameters.Length != 1)
        {
            return false;
        }

        if (!SymbolEqualityComparer.Default.Equals(parseMethod.ContainingType, backingType))
        {
            return false;
        }

        if (!SymbolEqualityComparer.Default.Equals(parseMethod.ReturnType, backingType))
        {
            return false;
        }

        var parameter = parseMethod.Parameters[0];
        return parameter.RefKind == RefKind.None &&
               SymbolEqualityComparer.Default.Equals(parameter.Type, types.StringType);
    }

    private static bool IsExactTryParseOnBackingType(
        IMethodSymbol tryParseMethod,
        ITypeSymbol backingType,
        AnalyzerTypeContext types)
    {
        if (!tryParseMethod.IsStatic ||
            tryParseMethod.Name != "TryParse" ||
            tryParseMethod.Parameters.Length != 2)
        {
            return false;
        }

        if (!SymbolEqualityComparer.Default.Equals(tryParseMethod.ContainingType, backingType))
        {
            return false;
        }

        if (!SymbolEqualityComparer.Default.Equals(tryParseMethod.ReturnType, types.BoolType))
        {
            return false;
        }

        var stringParam = tryParseMethod.Parameters[0];
        var outParam = tryParseMethod.Parameters[1];

        return stringParam.RefKind == RefKind.None &&
               SymbolEqualityComparer.Default.Equals(stringParam.Type, types.StringType) &&
               outParam.RefKind == RefKind.Out &&
               SymbolEqualityComparer.Default.Equals(outParam.Type, backingType);
    }

    private static bool IdTypeExposesParse(
        INamedTypeSymbol idType,
        AnalyzerTypeContext types)
    {
        foreach (var member in idType.GetMembers("Parse"))
        {
            if (member is not IMethodSymbol method ||
                !method.IsStatic ||
                method.DeclaredAccessibility != Accessibility.Public ||
                method.Parameters.Length != 1)
            {
                continue;
            }

            var parameter = method.Parameters[0];
            if (parameter.RefKind != RefKind.None ||
                !SymbolEqualityComparer.Default.Equals(parameter.Type, types.StringType))
            {
                continue;
            }

            if (SymbolEqualityComparer.Default.Equals(method.ReturnType, idType))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IdTypeExposesTryParse(
        INamedTypeSymbol idType,
        AnalyzerTypeContext types)
    {
        foreach (var member in idType.GetMembers("TryParse"))
        {
            if (member is not IMethodSymbol method ||
                !method.IsStatic ||
                method.DeclaredAccessibility != Accessibility.Public ||
                method.Parameters.Length != 2)
            {
                continue;
            }

            if (!SymbolEqualityComparer.Default.Equals(method.ReturnType, types.BoolType))
            {
                continue;
            }

            var stringParam = method.Parameters[0];
            var outParam = method.Parameters[1];

            if (stringParam.RefKind != RefKind.None ||
                !SymbolEqualityComparer.Default.Equals(stringParam.Type, types.StringType))
            {
                continue;
            }

            if (outParam.RefKind == RefKind.Out &&
                SymbolEqualityComparer.Default.Equals(outParam.Type, idType))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryGetOutVarTryParseInvocation(
        ILocalSymbol localSymbol,
        System.Threading.CancellationToken cancellationToken,
        out InvocationExpressionSyntax invocation)
    {
        invocation = null!;

        var declaringReference = localSymbol.DeclaringSyntaxReferences.FirstOrDefault();
        if (declaringReference is null)
        {
            return false;
        }

        var declaringSyntax = declaringReference.GetSyntax(cancellationToken);
        if (declaringSyntax is not SingleVariableDesignationSyntax designation)
        {
            return false;
        }

        if (designation.Parent is not DeclarationExpressionSyntax declarationExpression)
        {
            return false;
        }

        if (declarationExpression.Parent is not ArgumentSyntax argument)
        {
            return false;
        }

        if (!argument.RefKindKeyword.IsKind(SyntaxKind.OutKeyword))
        {
            return false;
        }

        if (argument.Parent is not ArgumentListSyntax argumentList)
        {
            return false;
        }

        if (argumentList.Parent is not InvocationExpressionSyntax invocationCandidate)
        {
            return false;
        }

        invocation = invocationCandidate;
        return true;
    }

    private static bool IsInsideSuccessBranchOf(
        InvocationExpressionSyntax tryParseInvocation,
        BaseObjectCreationExpressionSyntax creation,
        out StatementSyntax thenBranch)
    {
        thenBranch = null!;

        // Walk up from the invocation through parentheses (only) to find the
        // enclosing if statement. Any other intervening expression (e.g. `!`,
        // `&&`, `||`, comparison, conditional) means the TryParse is not the
        // direct success-gate of the if, so the rewrite would not preserve
        // semantics.
        SyntaxNode current = tryParseInvocation;
        while (current.Parent is ParenthesizedExpressionSyntax paren)
        {
            current = paren;
        }

        if (current.Parent is not IfStatementSyntax ifStatement)
        {
            return false;
        }

        if (ifStatement.Condition != current)
        {
            return false;
        }

        var ifContainingBranch = ifStatement.Statement;
        if (ifContainingBranch is null)
        {
            return false;
        }

        if (!creation.AncestorsAndSelf().Any(node => node == ifContainingBranch))
        {
            return false;
        }

        thenBranch = ifContainingBranch;
        return true;
    }

    private static bool CrossesLambdaOrLocalFunctionBoundary(
        BaseObjectCreationExpressionSyntax creation,
        StatementSyntax ifThenBranch)
    {
        foreach (var ancestor in creation.Ancestors())
        {
            if (ancestor == ifThenBranch)
            {
                return false;
            }

            switch (ancestor)
            {
                case AnonymousFunctionExpressionSyntax:
                case LocalFunctionStatementSyntax:
                    return true;
            }
        }

        return false;
    }

    private static bool LocalIsWrittenInsideBranch(
        SemanticModel semanticModel,
        StatementSyntax thenBranch,
        ILocalSymbol localSymbol)
    {
        var dataFlow = semanticModel.AnalyzeDataFlow(thenBranch);
        if (dataFlow is null || !dataFlow.Succeeded)
        {
            // If the analyzer cannot reason about the region, stay silent
            // rather than risk a false positive on a semantics-changing rewrite.
            return true;
        }

        foreach (var written in dataFlow.WrittenInside)
        {
            if (SymbolEqualityComparer.Default.Equals(written, localSymbol))
            {
                return true;
            }
        }

        return false;
    }

    private static ExpressionSyntax StripParentheses(ExpressionSyntax expression)
    {
        while (expression is ParenthesizedExpressionSyntax paren)
        {
            expression = paren.Expression;
        }

        return expression;
    }

    private sealed class AnalyzerTypeContext
    {
        public AnalyzerTypeContext(
            INamedTypeSymbol attributeType,
            INamedTypeSymbol stringType,
            INamedTypeSymbol boolType)
        {
            AttributeType = attributeType;
            StringType = stringType;
            BoolType = boolType;
        }

        public INamedTypeSymbol AttributeType { get; }

        public INamedTypeSymbol StringType { get; }

        public INamedTypeSymbol BoolType { get; }
    }
}
