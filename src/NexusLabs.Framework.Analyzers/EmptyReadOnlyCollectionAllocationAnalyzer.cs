using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace NexusLabs.Framework.Analyzers;

/// <summary>
/// Flags allocation of an empty collection (any constructed type — <c>new List&lt;T&gt;()</c>,
/// <c>new Dictionary&lt;TKey,TValue&gt;()</c>, <c>new HashSet&lt;T&gt;()</c>,
/// <c>new SortedDictionary&lt;TKey,TValue&gt;()</c>, <c>new ObservableCollection&lt;T&gt;()</c>,
/// a user-defined collection, …) when the instance is immediately widened to a read-only
/// collection interface (<c>IEnumerable&lt;T&gt;</c>, <c>IReadOnlyCollection&lt;T&gt;</c>,
/// <c>IReadOnlyList&lt;T&gt;</c>, <c>IReadOnlyDictionary&lt;TKey,TValue&gt;</c>,
/// <c>IReadOnlySet&lt;T&gt;</c>). Because the caller can never mutate the value through
/// that interface, a single shared empty instance should be used instead: <c>[]</c> for
/// the list family, <c>ReadOnlyDictionary&lt;TKey,TValue&gt;.Empty</c>, or
/// <c>FrozenSet&lt;T&gt;.Empty</c>.
/// <para>
/// The trigger keys on the <em>converted</em> type, so a creation assigned to a mutable
/// local and later populated is not flagged (its converted type is the concrete collection,
/// not the interface). The mutable interfaces (<c>IList&lt;T&gt;</c>, <c>ICollection&lt;T&gt;</c>,
/// <c>ISet&lt;T&gt;</c>, <c>IDictionary&lt;TKey,TValue&gt;</c>) are intentionally NOT covered
/// because their callers may add to the collection. Zero-length array allocations are covered
/// by the built-in CA1825. A type that must be constructed solely for a constructor side effect
/// (an anti-pattern for a collection) should suppress the call site with
/// <c>#pragma warning disable NLF0019</c>.
/// </para>
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class EmptyReadOnlyCollectionAllocationAnalyzer : DiagnosticAnalyzer
{
    private enum CollectionFamily
    {
        List,
        Dictionary,
        Set,
    }

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.DoNotAllocateEmptyReadOnlyCollection);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context)
    {
        var compilation = context.Compilation;

        var ienumerable = compilation.GetTypeByMetadataName("System.Collections.Generic.IEnumerable`1");
        var iReadOnlyCollection = compilation.GetTypeByMetadataName("System.Collections.Generic.IReadOnlyCollection`1");
        var iReadOnlyList = compilation.GetTypeByMetadataName("System.Collections.Generic.IReadOnlyList`1");
        var iReadOnlyDictionary = compilation.GetTypeByMetadataName("System.Collections.Generic.IReadOnlyDictionary`2");
        var iReadOnlySet = compilation.GetTypeByMetadataName("System.Collections.Generic.IReadOnlySet`1");

        var readOnlyDictionaryHasEmpty = HasEmptyMember(
            compilation.GetTypeByMetadataName("System.Collections.ObjectModel.ReadOnlyDictionary`2"));
        var frozenSetHasEmpty = HasEmptyMember(
            compilation.GetTypeByMetadataName("System.Collections.Frozen.FrozenSet`1"));

        context.RegisterSyntaxNodeAction(
            nodeContext => Analyze(
                nodeContext,
                ienumerable,
                iReadOnlyCollection,
                iReadOnlyList,
                iReadOnlyDictionary,
                iReadOnlySet,
                readOnlyDictionaryHasEmpty,
                frozenSetHasEmpty),
            SyntaxKind.ObjectCreationExpression);
    }

    private static void Analyze(
        SyntaxNodeAnalysisContext context,
        INamedTypeSymbol? ienumerable,
        INamedTypeSymbol? iReadOnlyCollection,
        INamedTypeSymbol? iReadOnlyList,
        INamedTypeSymbol? iReadOnlyDictionary,
        INamedTypeSymbol? iReadOnlySet,
        bool readOnlyDictionaryHasEmpty,
        bool frozenSetHasEmpty)
    {
        var creation = (ObjectCreationExpressionSyntax)context.Node;

        if (!IsEmptyCreation(creation))
        {
            return;
        }

        var typeInfo = context.SemanticModel.GetTypeInfo(creation, context.CancellationToken);
        if (typeInfo.Type is not INamedTypeSymbol createdType ||
            typeInfo.ConvertedType is not INamedTypeSymbol convertedType)
        {
            return;
        }

        if (SymbolEqualityComparer.Default.Equals(createdType, convertedType))
        {
            return;
        }

        var convertedDefinition = convertedType.OriginalDefinition;
        var family = ResolveFamily(
            convertedDefinition,
            ienumerable,
            iReadOnlyCollection,
            iReadOnlyList,
            iReadOnlyDictionary,
            iReadOnlySet);
        if (family is null)
        {
            return;
        }

        string replacement;
        switch (family.Value)
        {
            case CollectionFamily.List:
                replacement = BuildListReplacement(convertedType);
                break;
            case CollectionFamily.Dictionary:
                if (!readOnlyDictionaryHasEmpty)
                {
                    return;
                }

                replacement = $"`ReadOnlyDictionary<{FormatTypeArguments(convertedType)}>.Empty`";
                break;
            case CollectionFamily.Set:
                if (!frozenSetHasEmpty)
                {
                    return;
                }

                replacement = $"`FrozenSet<{FormatTypeArguments(convertedType)}>.Empty`";
                break;
            default:
                return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.DoNotAllocateEmptyReadOnlyCollection,
            creation.Type.GetLocation(),
            "new " + creation.Type.ToString() + "()",
            convertedType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
            replacement));
    }

    private static CollectionFamily? ResolveFamily(
        INamedTypeSymbol convertedDefinition,
        INamedTypeSymbol? ienumerable,
        INamedTypeSymbol? iReadOnlyCollection,
        INamedTypeSymbol? iReadOnlyList,
        INamedTypeSymbol? iReadOnlyDictionary,
        INamedTypeSymbol? iReadOnlySet)
    {
        if (SymbolEqualityComparer.Default.Equals(convertedDefinition, iReadOnlyList) ||
            SymbolEqualityComparer.Default.Equals(convertedDefinition, iReadOnlyCollection) ||
            SymbolEqualityComparer.Default.Equals(convertedDefinition, ienumerable))
        {
            return CollectionFamily.List;
        }

        if (SymbolEqualityComparer.Default.Equals(convertedDefinition, iReadOnlyDictionary))
        {
            return CollectionFamily.Dictionary;
        }

        if (SymbolEqualityComparer.Default.Equals(convertedDefinition, iReadOnlySet))
        {
            return CollectionFamily.Set;
        }

        return null;
    }

    private static string BuildListReplacement(INamedTypeSymbol convertedType)
    {
        if (convertedType.TypeArguments.Length == 1)
        {
            var element = convertedType.TypeArguments[0].ToDisplayString();
            return $"the collection expression `[]` (equivalently `Array.Empty<{element}>()`)";
        }

        return "the collection expression `[]` (equivalently `Array.Empty<T>()`)";
    }

    private static string FormatTypeArguments(INamedTypeSymbol type) =>
        string.Join(", ", type.TypeArguments.Select(static argument => argument.ToDisplayString()));

    private static bool HasEmptyMember(INamedTypeSymbol? type) =>
        type?.GetMembers("Empty").Any(static member => member is IPropertySymbol or IFieldSymbol) == true;

    private static bool IsEmptyCreation(ObjectCreationExpressionSyntax creation)
    {
        var hasArguments = creation.ArgumentList is { Arguments.Count: > 0 };
        var hasInitializerElements = creation.Initializer is { Expressions.Count: > 0 };
        return !hasArguments && !hasInitializerElements;
    }
}
