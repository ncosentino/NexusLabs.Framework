using System.Collections.Immutable;
using System.Linq;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace NexusLabs.Framework.Analyzers;

/// <summary>
/// Flags classes that implement <c>Carter.ICarterModule</c> but are not declared
/// <c>public sealed class</c>. Carter's reflection-based module discovery
/// enumerates only PUBLIC types implementing the interface, so an
/// <c>internal</c> module compiles cleanly but is silently skipped at startup
/// — every route it declares returns 404 at runtime with no build-time or
/// startup-time error to surface the misconfiguration.
/// </summary>
/// <remarks>
/// The analyzer matches by symbol metadata in the consumer's compilation, so
/// the analyzer assembly itself takes no dependency on Carter. The match
/// requires both <c>ContainingNamespace.ToDisplayString() == "Carter"</c> and
/// <c>Name == "ICarterModule"</c> on a type that appears in the candidate's
/// <c>AllInterfaces</c> — same-named interfaces in other namespaces do not
/// trigger the rule.
/// <para>
/// Partial declarations are handled correctly because the rule keys off the
/// symbol's aggregate modifiers, not any one syntax declaration. The
/// diagnostic is reported at the identifier of the first
/// <see cref="ClassDeclarationSyntax"/> reference if available, otherwise the
/// symbol's first declared location.
/// </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class CarterModuleVisibilityAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.CarterModuleMustBePublicSealedClass);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
    }

    private static void AnalyzeNamedType(SymbolAnalysisContext context)
    {
        var namedType = (INamedTypeSymbol)context.Symbol;

        if (namedType.TypeKind != TypeKind.Class)
        {
            return;
        }

        if (!ImplementsCarterModule(namedType))
        {
            return;
        }

        var isPublic = namedType.DeclaredAccessibility == Accessibility.Public;
        var isSealed = namedType.IsSealed;

        if (isPublic && isSealed)
        {
            return;
        }

        var location = GetReportLocation(namedType, context.CancellationToken);
        if (location is null)
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.CarterModuleMustBePublicSealedClass,
            location,
            namedType.Name,
            DescribeActualDeclaration(namedType)));
    }

    private static bool ImplementsCarterModule(INamedTypeSymbol namedType)
    {
        foreach (var iface in namedType.AllInterfaces)
        {
            if (iface.Name == "ICarterModule" &&
                iface.ContainingNamespace?.ToDisplayString() == "Carter")
            {
                return true;
            }
        }

        return false;
    }

    private static string DescribeActualDeclaration(INamedTypeSymbol namedType)
    {
        var accessibility = namedType.DeclaredAccessibility switch
        {
            Accessibility.Public => "public",
            Accessibility.Internal => "internal",
            Accessibility.Private => "private",
            Accessibility.Protected => "protected",
            Accessibility.ProtectedOrInternal => "protected internal",
            Accessibility.ProtectedAndInternal => "private protected",
            _ => "internal",
        };

        var classKind = namedType.IsAbstract
            ? "abstract class"
            : namedType.IsSealed
                ? "sealed class"
                : "class";

        return $"{accessibility} {classKind}";
    }

    private static Location? GetReportLocation(
        INamedTypeSymbol namedType,
        CancellationToken cancellationToken)
    {
        foreach (var reference in namedType.DeclaringSyntaxReferences)
        {
            var syntax = reference.GetSyntax(cancellationToken);
            if (syntax is ClassDeclarationSyntax classDeclaration)
            {
                return classDeclaration.Identifier.GetLocation();
            }
        }

        return namedType.Locations.FirstOrDefault();
    }
}
