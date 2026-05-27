using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace NexusLabs.Framework.Analyzers;

/// <summary>
/// Reports parameterless <c>[NexusLabs.Framework.TransfersOwnership]</c>
/// annotations on members whose type is not <see cref="System.IDisposable"/>
/// or <see cref="System.IAsyncDisposable"/>. Such annotations are silently
/// ignored by <c>TransfersOwnershipDisposeSuppressor</c> (NLFSUP001) and
/// represent a developer expecting suppression that will not occur.
/// </summary>
/// <remarks>
/// <para>
/// The attribute has two valid shapes:
/// </para>
/// <list type="bullet">
/// <item>
/// <description>
/// <strong>Shape B</strong> — parameterless on a disposable field, property,
/// or parameter. Authorises disposal of THAT member.
/// </description>
/// </item>
/// <item>
/// <description>
/// <strong>Shape A</strong> — with one or more target names on a flag /
/// parameter that gates conditional ownership. Authorises disposal of the
/// listed targets inside the guarded <c>if</c> body.
/// </description>
/// </item>
/// </list>
/// <para>
/// A parameterless attribute on a member whose type is not disposable
/// matches neither shape and produces no suppression. NLF0012 makes that
/// silent footgun visible at build time.
/// </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TransfersOwnershipInertAnalyzer : DiagnosticAnalyzer
{
    private const string TransfersOwnershipAttributeMetadataName =
        "NexusLabs.Framework.TransfersOwnershipAttribute";

    private const string DisposableMetadataName = "System.IDisposable";
    private const string AsyncDisposableMetadataName = "System.IAsyncDisposable";

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.TransfersOwnershipInertOnNonDisposable);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context)
    {
        var compilation = context.Compilation;
        var attributeType = compilation.GetTypeByMetadataName(TransfersOwnershipAttributeMetadataName);
        if (attributeType is null)
        {
            return;
        }

        var disposable = compilation.GetTypeByMetadataName(DisposableMetadataName);
        var asyncDisposable = compilation.GetTypeByMetadataName(AsyncDisposableMetadataName);

        var analysisContext = new AnalyzerTypeContext(attributeType, disposable, asyncDisposable);

        context.RegisterSymbolAction(
            symbolContext => AnalyzeSymbol(symbolContext, analysisContext),
            SymbolKind.Field,
            SymbolKind.Property,
            SymbolKind.Parameter);
    }

    private static void AnalyzeSymbol(
        SymbolAnalysisContext context,
        AnalyzerTypeContext types)
    {
        var attribute = FindTransfersOwnershipAttribute(context.Symbol, types.AttributeType);
        if (attribute is null)
        {
            return;
        }

        if (!HasNoTargets(attribute))
        {
            return;
        }

        var memberType = GetMemberType(context.Symbol);
        if (memberType is null)
        {
            return;
        }

        if (IsOrImplements(memberType, types.Disposable) ||
            IsOrImplements(memberType, types.AsyncDisposable))
        {
            return;
        }

        var location = GetAttributeLocation(attribute, context.CancellationToken)
            ?? context.Symbol.Locations.FirstOrDefault();
        if (location is null)
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.TransfersOwnershipInertOnNonDisposable,
            location,
            context.Symbol.Name));
    }

    private static AttributeData? FindTransfersOwnershipAttribute(
        ISymbol symbol,
        INamedTypeSymbol attributeType)
    {
        foreach (var attribute in symbol.GetAttributes())
        {
            if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, attributeType))
            {
                return attribute;
            }
        }

        return null;
    }

    private static bool HasNoTargets(AttributeData attribute)
    {
        if (attribute.ConstructorArguments.Length == 0)
        {
            return true;
        }

        var arg = attribute.ConstructorArguments[0];
        if (arg.Kind != TypedConstantKind.Array)
        {
            return false;
        }

        return arg.IsNull || arg.Values.IsDefaultOrEmpty;
    }

    private static ITypeSymbol? GetMemberType(ISymbol symbol) => symbol switch
    {
        IFieldSymbol field => field.Type,
        IPropertySymbol property => property.Type,
        IParameterSymbol parameter => parameter.Type,
        _ => null,
    };

    private static bool IsOrImplements(ITypeSymbol type, INamedTypeSymbol? interfaceType)
    {
        if (interfaceType is null)
        {
            return false;
        }

        if (SymbolEqualityComparer.Default.Equals(type, interfaceType))
        {
            return true;
        }

        foreach (var implemented in type.AllInterfaces)
        {
            if (SymbolEqualityComparer.Default.Equals(implemented, interfaceType))
            {
                return true;
            }
        }

        if (type is ITypeParameterSymbol typeParameter)
        {
            foreach (var constraint in typeParameter.ConstraintTypes)
            {
                if (IsOrImplements(constraint, interfaceType))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static Location? GetAttributeLocation(
        AttributeData attribute,
        System.Threading.CancellationToken cancellationToken)
    {
        var reference = attribute.ApplicationSyntaxReference;
        return reference?.GetSyntax(cancellationToken).GetLocation();
    }

    private sealed class AnalyzerTypeContext
    {
        public AnalyzerTypeContext(
            INamedTypeSymbol attributeType,
            INamedTypeSymbol? disposable,
            INamedTypeSymbol? asyncDisposable)
        {
            AttributeType = attributeType;
            Disposable = disposable;
            AsyncDisposable = asyncDisposable;
        }

        public INamedTypeSymbol AttributeType { get; }

        public INamedTypeSymbol? Disposable { get; }

        public INamedTypeSymbol? AsyncDisposable { get; }
    }
}
