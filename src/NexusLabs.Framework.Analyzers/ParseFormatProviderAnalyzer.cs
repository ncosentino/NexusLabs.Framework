using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace NexusLabs.Framework.Analyzers;

/// <summary>
/// Flags calls to a static <c>Parse</c> / <c>TryParse</c> method on a type
/// that exposes a culture-aware sibling overload (same parameter list plus
/// an additional <see cref="System.IFormatProvider"/> parameter) but is
/// being invoked through an overload that does not specify a culture
/// (NLF0014).
/// </summary>
/// <remarks>
/// <para>
/// Implicit-culture parsing is one of the most common sources of
/// locale-dependent bugs in .NET — number formats, decimal separators,
/// date layouts, and so on are all driven by the running thread's
/// <c>CurrentCulture</c> when an <c>IFormatProvider</c> is omitted. This
/// analyzer makes the omission visible at build time wherever a
/// culture-aware upgrade path exists.
/// </para>
/// <para>
/// The match is a strict "compatible upgrade": the sibling overload must
/// have the same name, the same return type, be static, public, and have
/// a parameter list that is the called method's parameter list with
/// exactly one additional <c>IFormatProvider</c> parameter inserted at
/// any position. Other overloads that exist on the type — for example
/// one that also requires a <c>NumberStyles</c> the caller did not supply
/// — do not trigger the diagnostic, because the caller cannot upgrade
/// in a single step without additional information.
/// </para>
/// <para>
/// Calls whose called overload already accepts an <c>IFormatProvider</c>
/// (or any subtype, e.g. <c>CultureInfo</c>) are silent: the caller is
/// being explicit at the API level, regardless of whether they pass
/// <c>null</c> or a real provider at the call site. Extension methods,
/// instance methods, and methods on types loaded from a compilation
/// without <see cref="System.IFormatProvider"/> are also silent.
/// </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ParseFormatProviderAnalyzer : DiagnosticAnalyzer
{
    private const string FormatProviderMetadataName = "System.IFormatProvider";

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.ParseTryParseMissingFormatProvider);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context)
    {
        var formatProviderType = context.Compilation.GetTypeByMetadataName(FormatProviderMetadataName);
        if (formatProviderType is null)
        {
            return;
        }

        context.RegisterSyntaxNodeAction(
            ctx => AnalyzeInvocation(ctx, formatProviderType),
            SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeInvocation(
        SyntaxNodeAnalysisContext context,
        INamedTypeSymbol formatProviderType)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        if (context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol
            is not IMethodSymbol called)
        {
            return;
        }

        if (!called.IsStatic ||
            called.IsExtensionMethod ||
            called.MethodKind != MethodKind.Ordinary)
        {
            return;
        }

        if (called.Name != "Parse" && called.Name != "TryParse")
        {
            return;
        }

        if (CalledMethodAlreadyHasFormatProvider(called, formatProviderType))
        {
            return;
        }

        if (!ContainingTypeHasCompatibleFormatProviderOverload(called, formatProviderType))
        {
            return;
        }

        var diagnosticLocation = GetCalledMethodLocation(invocation);

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.ParseTryParseMissingFormatProvider,
            diagnosticLocation,
            called.ContainingType.ToDisplayString(),
            called.Name));
    }

    private static bool CalledMethodAlreadyHasFormatProvider(
        IMethodSymbol called,
        INamedTypeSymbol formatProviderType)
    {
        foreach (var parameter in called.Parameters)
        {
            if (ParameterIsOrImplementsFormatProvider(parameter, formatProviderType))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainingTypeHasCompatibleFormatProviderOverload(
        IMethodSymbol called,
        INamedTypeSymbol formatProviderType)
    {
        var containingType = called.ContainingType;
        if (containingType is null)
        {
            return false;
        }

        foreach (var member in containingType.GetMembers(called.Name))
        {
            if (member is not IMethodSymbol candidate ||
                !candidate.IsStatic ||
                candidate.DeclaredAccessibility != Accessibility.Public ||
                candidate.MethodKind != MethodKind.Ordinary)
            {
                continue;
            }

            if (SymbolEqualityComparer.Default.Equals(candidate, called))
            {
                continue;
            }

            if (!SymbolEqualityComparer.Default.Equals(candidate.ReturnType, called.ReturnType))
            {
                continue;
            }

            if (IsCompatibleFormatProviderUpgrade(called, candidate, formatProviderType))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsCompatibleFormatProviderUpgrade(
        IMethodSymbol called,
        IMethodSymbol candidate,
        INamedTypeSymbol formatProviderType)
    {
        if (candidate.Parameters.Length != called.Parameters.Length + 1)
        {
            return false;
        }

        var calledIndex = 0;
        var sawFormatProviderInsert = false;

        for (var candidateIndex = 0; candidateIndex < candidate.Parameters.Length; candidateIndex++)
        {
            var candidateParam = candidate.Parameters[candidateIndex];

            if (!sawFormatProviderInsert &&
                ParameterIsOrImplementsFormatProvider(candidateParam, formatProviderType) &&
                (calledIndex >= called.Parameters.Length ||
                 !ParametersMatch(called.Parameters[calledIndex], candidateParam)))
            {
                sawFormatProviderInsert = true;
                continue;
            }

            if (calledIndex >= called.Parameters.Length)
            {
                return false;
            }

            if (!ParametersMatch(called.Parameters[calledIndex], candidateParam))
            {
                return false;
            }

            calledIndex++;
        }

        return sawFormatProviderInsert && calledIndex == called.Parameters.Length;
    }

    private static bool ParametersMatch(IParameterSymbol a, IParameterSymbol b) =>
        a.RefKind == b.RefKind &&
        SymbolEqualityComparer.Default.Equals(a.Type, b.Type);

    private static bool ParameterIsOrImplementsFormatProvider(
        IParameterSymbol parameter,
        INamedTypeSymbol formatProviderType)
    {
        if (parameter.RefKind == RefKind.Out)
        {
            return false;
        }

        return TypeIsOrImplementsFormatProvider(parameter.Type, formatProviderType);
    }

    private static bool TypeIsOrImplementsFormatProvider(
        ITypeSymbol type,
        INamedTypeSymbol formatProviderType)
    {
        if (SymbolEqualityComparer.Default.Equals(type, formatProviderType))
        {
            return true;
        }

        foreach (var implemented in type.AllInterfaces)
        {
            if (SymbolEqualityComparer.Default.Equals(implemented, formatProviderType))
            {
                return true;
            }
        }

        return false;
    }

    private static Location GetCalledMethodLocation(InvocationExpressionSyntax invocation)
    {
        // Point at the method-name part of the invocation (e.g. `Parse` /
        // `TryParse`) so the squiggle lands on the call selector rather than
        // the entire argument list.
        return invocation.Expression switch
        {
            MemberAccessExpressionSyntax member => member.Name.GetLocation(),
            _ => invocation.Expression.GetLocation(),
        };
    }
}
