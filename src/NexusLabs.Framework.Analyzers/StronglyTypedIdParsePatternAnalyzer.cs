using System.Collections.Immutable;
using System.Linq;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace NexusLabs.Framework.Analyzers;

/// <summary>
/// Flags the awkward "parse via backing type, then construct the ID" pattern
/// for types decorated with
/// <c>[StronglyTypedIds.StronglyTypedIdAttribute]</c> — or for types that
/// match the same structural shape but inherit from a metadata reference
/// where the attribute was stripped (e.g. when the attribute is
/// <c>[Conditional("STRONGLY_TYPED_ID_USAGES")]</c> and the constant is
/// not defined in the producing project). NLF0013.
/// </summary>
/// <remarks>
/// <para>
/// Two shapes are detected, both gated on (a) the constructed type being
/// recognised as a strongly-typed ID and (b) that ID exposing a static
/// parser whose parameters match the backing-type call being replaced:
/// </para>
/// <list type="bullet">
///   <item>
///     <description>
///     <strong>Parse:</strong>
///     <c>new XxxId(BackingType.Parse(s, …))</c>. The matched overload may
///     have any additional arguments (e.g. <c>IFormatProvider</c>,
///     <c>NumberStyles</c>) as long as the strongly-typed ID exposes the
///     same overload. The fix is the equivalent
///     <c>XxxId.Parse(s, …)</c>.
///     </description>
///   </item>
///   <item>
///     <description>
///     <strong>TryParse:</strong> the construction <c>new XxxId(g)</c>
///     lives inside the success branch of an <c>if</c> whose entire
///     condition is <c>BackingType.TryParse(s, …, out X)</c>. <c>X</c> may
///     be <c>out var g</c>, <c>out BackingType g</c>, or a reference
///     <c>out g</c> to a predeclared local — all three forms are matched
///     uniformly by resolving the out argument to its <c>ILocalSymbol</c>
///     and comparing against the constructor argument's local. The fix is
///     the equivalent <c>if (XxxId.TryParse(s, …, out var id)) …</c>.
///     </description>
///   </item>
/// </list>
/// <para>
/// The TryParse arm is intentionally conservative: it only fires when (a)
/// the <c>TryParse</c> invocation is the entire condition of an enclosing
/// <c>if</c> (no negation, no compound boolean, no nesting inside a larger
/// expression), (b) the construction lives inside that <c>if</c>'s
/// success branch (never the <c>else</c>), (c) the local is never
/// written to inside the success branch (using
/// <see cref="SemanticModel.AnalyzeDataFlow(SyntaxNode)"/>), and (d) no
/// lambda or local function boundary separates the construction from the
/// <c>if</c>.
/// </para>
/// <para>
/// Detection that a type is "strongly-typed-id-like" follows two paths:
/// </para>
/// <list type="number">
///   <item>
///     <description>
///     <strong>Attribute path:</strong> the type carries
///     <c>[StronglyTypedIds.StronglyTypedIdAttribute]</c>. This is the
///     primary signal and works for any type defined in source.
///     </description>
///   </item>
///   <item>
///     <description>
///     <strong>Structural fallback (metadata-only):</strong> when the type
///     is loaded from metadata (a referenced assembly) and does NOT carry
///     the attribute — for example because the producing project stripped
///     it via <c>[Conditional]</c> — the analyzer falls back to a strict
///     structural check: the type must be a value type defined outside
///     the BCL, the invoked constructor must be public and take a single
///     parameter of the backing type, the type must expose a public
///     instance property of that same backing type (any name; usually
///     <c>Value</c>), and the type must expose the matching static parser
///     overload. Source-declared types without the attribute are
///     <em>never</em> caught by the fallback — if the developer chose not
///     to decorate the type, that decision is respected.
///     </description>
///   </item>
/// </list>
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
        // The attribute may not be present in this compilation (consumer
        // project references the ID type from a project that uses the
        // StronglyTypedId package without flowing the attribute through).
        // Register the syntax action regardless; the structural fallback
        // covers the metadata-only path.
        var attributeType = context.Compilation.GetTypeByMetadataName(AttributeMetadataName);
        var stringType = context.Compilation.GetSpecialType(SpecialType.System_String);
        var boolType = context.Compilation.GetSpecialType(SpecialType.System_Boolean);

        var types = new AnalyzerTypeContext(
            attributeType,
            stringType,
            boolType,
            context.Compilation.Assembly);

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

        if (!argument.RefKindKeyword.IsKind(SyntaxKind.None))
        {
            return;
        }

        if (context.SemanticModel.GetSymbolInfo(creation, context.CancellationToken).Symbol
            is not IMethodSymbol ctor)
        {
            return;
        }

        if (ctor.MethodKind != MethodKind.Constructor ||
            ctor.DeclaredAccessibility != Accessibility.Public ||
            ctor.Parameters.Length != 1)
        {
            return;
        }

        var idType = ctor.ContainingType;
        if (idType is null)
        {
            return;
        }

        var backingType = ctor.Parameters[0].Type;
        if (backingType is null)
        {
            return;
        }

        if (!IsStronglyTypedIdLike(idType, backingType, types))
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

        if (!IsParseOnBackingType(parseMethod, backingType, types))
        {
            return false;
        }

        if (!IdTypeHasMatchingParseOverload(idType, parseMethod))
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

        if (!TryFindGuardingTryParseIf(
                context.SemanticModel,
                creation,
                localSymbol,
                backingType,
                types,
                context.CancellationToken,
                out var ifStatement,
                out var tryParseMethod))
        {
            return;
        }

        if (!IdTypeHasMatchingTryParseOverload(idType, tryParseMethod!, types))
        {
            return;
        }

        var thenBranch = ifStatement!.Statement;
        if (thenBranch is null)
        {
            return;
        }

        if (CrossesLambdaOrLocalFunctionBoundary(creation, thenBranch))
        {
            return;
        }

        if (LocalIsWrittenAnywhereInBranch(context.SemanticModel, thenBranch, localSymbol))
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

    private static bool IsStronglyTypedIdLike(
        INamedTypeSymbol idType,
        ITypeSymbol backingType,
        AnalyzerTypeContext types)
    {
        if (types.AttributeType is not null && HasAttribute(idType, types.AttributeType))
        {
            return true;
        }

        // Structural fallback only fires for types declared in a DIFFERENT
        // assembly than the one currently being compiled — i.e. cross-project
        // references where the [StronglyTypedId] attribute was stripped via
        // [Conditional] or the attribute assembly is not referenced by this
        // project. Types declared in source within the same assembly without
        // the attribute are not strongly-typed IDs and the developer's
        // decision to omit the attribute is respected.
        if (SymbolEqualityComparer.Default.Equals(idType.ContainingAssembly, types.CurrentAssembly))
        {
            return false;
        }

        if (!idType.IsValueType)
        {
            return false;
        }

        if (IsLikelyBclAssembly(idType.ContainingAssembly))
        {
            return false;
        }

        if (!HasPublicPropertyOfType(idType, backingType))
        {
            return false;
        }

        return true;
    }

    private static bool HasAttribute(
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

    private static bool HasPublicPropertyOfType(
        INamedTypeSymbol idType,
        ITypeSymbol backingType)
    {
        foreach (var member in idType.GetMembers())
        {
            if (member is IPropertySymbol property &&
                property.DeclaredAccessibility == Accessibility.Public &&
                !property.IsStatic &&
                SymbolEqualityComparer.Default.Equals(property.Type, backingType))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsLikelyBclAssembly(IAssemblySymbol? assembly)
    {
        var name = assembly?.Name;
        if (name is null)
        {
            return false;
        }

        return name.StartsWith("System", System.StringComparison.Ordinal) ||
               name.StartsWith("Microsoft", System.StringComparison.Ordinal) ||
               name.Equals("mscorlib", System.StringComparison.Ordinal) ||
               name.Equals("netstandard", System.StringComparison.Ordinal);
    }

    private static bool IsParseOnBackingType(
        IMethodSymbol parseMethod,
        ITypeSymbol backingType,
        AnalyzerTypeContext types)
    {
        if (!parseMethod.IsStatic ||
            parseMethod.Name != "Parse" ||
            parseMethod.Parameters.Length < 1)
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

        var firstParam = parseMethod.Parameters[0];
        return firstParam.RefKind == RefKind.None &&
               SymbolEqualityComparer.Default.Equals(firstParam.Type, types.StringType);
    }

    private static bool IsTryParseOnBackingType(
        IMethodSymbol tryParseMethod,
        ITypeSymbol backingType,
        AnalyzerTypeContext types)
    {
        if (!tryParseMethod.IsStatic ||
            tryParseMethod.Name != "TryParse" ||
            tryParseMethod.Parameters.Length < 2)
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

        var firstParam = tryParseMethod.Parameters[0];
        if (firstParam.RefKind != RefKind.None ||
            !SymbolEqualityComparer.Default.Equals(firstParam.Type, types.StringType))
        {
            return false;
        }

        var lastParam = tryParseMethod.Parameters[tryParseMethod.Parameters.Length - 1];
        return lastParam.RefKind == RefKind.Out &&
               SymbolEqualityComparer.Default.Equals(lastParam.Type, backingType);
    }

    private static bool IdTypeHasMatchingParseOverload(
        INamedTypeSymbol idType,
        IMethodSymbol backingParseMethod)
    {
        foreach (var member in idType.GetMembers("Parse"))
        {
            if (member is not IMethodSymbol method ||
                !method.IsStatic ||
                method.DeclaredAccessibility != Accessibility.Public)
            {
                continue;
            }

            if (!SymbolEqualityComparer.Default.Equals(method.ReturnType, idType))
            {
                continue;
            }

            if (ParameterListsMatchExactly(method.Parameters, backingParseMethod.Parameters))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IdTypeHasMatchingTryParseOverload(
        INamedTypeSymbol idType,
        IMethodSymbol backingTryParseMethod,
        AnalyzerTypeContext types)
    {
        foreach (var member in idType.GetMembers("TryParse"))
        {
            if (member is not IMethodSymbol method ||
                !method.IsStatic ||
                method.DeclaredAccessibility != Accessibility.Public)
            {
                continue;
            }

            if (!SymbolEqualityComparer.Default.Equals(method.ReturnType, types.BoolType))
            {
                continue;
            }

            if (method.Parameters.Length != backingTryParseMethod.Parameters.Length)
            {
                continue;
            }

            var allButLastMatch = true;
            for (var i = 0; i < method.Parameters.Length - 1; i++)
            {
                if (!ParametersMatch(method.Parameters[i], backingTryParseMethod.Parameters[i]))
                {
                    allButLastMatch = false;
                    break;
                }
            }

            if (!allButLastMatch)
            {
                continue;
            }

            var lastIdParam = method.Parameters[method.Parameters.Length - 1];
            if (lastIdParam.RefKind == RefKind.Out &&
                SymbolEqualityComparer.Default.Equals(lastIdParam.Type, idType))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ParameterListsMatchExactly(
        ImmutableArray<IParameterSymbol> a,
        ImmutableArray<IParameterSymbol> b)
    {
        if (a.Length != b.Length)
        {
            return false;
        }

        for (var i = 0; i < a.Length; i++)
        {
            if (!ParametersMatch(a[i], b[i]))
            {
                return false;
            }
        }

        return true;
    }

    private static bool ParametersMatch(IParameterSymbol a, IParameterSymbol b) =>
        a.RefKind == b.RefKind &&
        SymbolEqualityComparer.Default.Equals(a.Type, b.Type);

    private static bool TryFindGuardingTryParseIf(
        SemanticModel semanticModel,
        BaseObjectCreationExpressionSyntax creation,
        ILocalSymbol localSymbol,
        ITypeSymbol backingType,
        AnalyzerTypeContext types,
        CancellationToken cancellationToken,
        out IfStatementSyntax? matchedIf,
        out IMethodSymbol? tryParseMethod)
    {
        matchedIf = null;
        tryParseMethod = null;

        // Walk up from the construction. For every IfStatement encountered,
        // check whether the construction lives in that if's success branch
        // (Statement) and whether the condition is BackingType.TryParse(s,
        // …, out X) where X resolves to the same local as the construction
        // argument. Lambdas and local functions terminate the walk because
        // capturing across those boundaries changes semantics.
        foreach (var ancestor in creation.Ancestors())
        {
            if (ancestor is AnonymousFunctionExpressionSyntax ||
                ancestor is LocalFunctionStatementSyntax)
            {
                return false;
            }

            if (ancestor is not IfStatementSyntax candidate)
            {
                continue;
            }

            if (candidate.Statement is null ||
                !IsInsideNode(creation, candidate.Statement))
            {
                continue;
            }

            if (!TryGetMatchingTryParseCondition(
                    candidate.Condition,
                    localSymbol,
                    backingType,
                    semanticModel,
                    types,
                    cancellationToken,
                    out tryParseMethod))
            {
                continue;
            }

            matchedIf = candidate;
            return true;
        }

        return false;
    }

    private static bool TryGetMatchingTryParseCondition(
        ExpressionSyntax condition,
        ILocalSymbol expectedLocal,
        ITypeSymbol backingType,
        SemanticModel semanticModel,
        AnalyzerTypeContext types,
        CancellationToken cancellationToken,
        out IMethodSymbol? tryParseMethod)
    {
        tryParseMethod = null;

        var stripped = StripParentheses(condition);
        if (stripped is not InvocationExpressionSyntax invocation)
        {
            return false;
        }

        if (semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol
            is not IMethodSymbol method)
        {
            return false;
        }

        if (!IsTryParseOnBackingType(method, backingType, types))
        {
            return false;
        }

        if (invocation.ArgumentList is null ||
            invocation.ArgumentList.Arguments.Count != method.Parameters.Length)
        {
            return false;
        }

        var outArg = invocation.ArgumentList.Arguments[invocation.ArgumentList.Arguments.Count - 1];
        if (!outArg.RefKindKeyword.IsKind(SyntaxKind.OutKeyword))
        {
            return false;
        }

        var outArgLocal = ResolveOutArgumentLocal(outArg, semanticModel, cancellationToken);
        if (outArgLocal is null)
        {
            return false;
        }

        if (!SymbolEqualityComparer.Default.Equals(outArgLocal, expectedLocal))
        {
            return false;
        }

        tryParseMethod = method;
        return true;
    }

    private static ILocalSymbol? ResolveOutArgumentLocal(
        ArgumentSyntax outArg,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        // Form 1: `out var g` or `out Guid g` — DeclarationExpression with a
        // SingleVariableDesignation that introduces a new local.
        if (outArg.Expression is DeclarationExpressionSyntax declExpr &&
            declExpr.Designation is SingleVariableDesignationSyntax svDesignation)
        {
            return semanticModel.GetDeclaredSymbol(svDesignation, cancellationToken) as ILocalSymbol;
        }

        // Form 2: `out g` — identifier referencing a predeclared local.
        if (outArg.Expression is IdentifierNameSyntax identName)
        {
            return semanticModel.GetSymbolInfo(identName, cancellationToken).Symbol as ILocalSymbol;
        }

        // `out _` (discard) yields IDiscardSymbol and never matches a local.
        return null;
    }

    private static bool IsInsideNode(SyntaxNode candidate, SyntaxNode container)
    {
        SyntaxNode? current = candidate;
        while (current is not null)
        {
            if (current == container)
            {
                return true;
            }
            current = current.Parent;
        }
        return false;
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

    private static bool LocalIsWrittenAnywhereInBranch(
        SemanticModel semanticModel,
        StatementSyntax thenBranch,
        ILocalSymbol localSymbol)
    {
        var dataFlow = semanticModel.AnalyzeDataFlow(thenBranch);
        if (dataFlow is null || !dataFlow.Succeeded)
        {
            // Be conservative if data-flow analysis fails — the rewrite
            // suggestion only holds if we can prove the local is not
            // rewritten inside the branch.
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
            INamedTypeSymbol? attributeType,
            INamedTypeSymbol stringType,
            INamedTypeSymbol boolType,
            IAssemblySymbol currentAssembly)
        {
            AttributeType = attributeType;
            StringType = stringType;
            BoolType = boolType;
            CurrentAssembly = currentAssembly;
        }

        public INamedTypeSymbol? AttributeType { get; }

        public INamedTypeSymbol StringType { get; }

        public INamedTypeSymbol BoolType { get; }

        public IAssemblySymbol CurrentAssembly { get; }
    }
}
