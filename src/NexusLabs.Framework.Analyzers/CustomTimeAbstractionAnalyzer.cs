using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace NexusLabs.Framework.Analyzers;

/// <summary>
/// Flags an interface that reimplements <c>System.TimeProvider</c>. Every member must be time
/// related and at least one must be a clock, so the common clock-plus-delay shape is reported
/// while a broader abstraction that merely exposes a timestamp is not.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class CustomTimeAbstractionAnalyzer : DiagnosticAnalyzer
{
    private static readonly string[] _clockNames =
    [
        "Now",
        "UtcNow",
        "GetNow",
        "GetUtcNow",
        "GetLocalNow",
        "CurrentTime",
        "GetTimestamp",
    ];

    private static readonly string[] _delayNames =
    [
        "Delay",
        "DelayAsync",
        "Sleep",
        "SleepAsync",
        "WaitAsync",
        "CreateTimer",
    ];

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.DoNotDefineCustomTimeAbstraction);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSymbolAction(Analyze, SymbolKind.NamedType);
    }

    private static void Analyze(SymbolAnalysisContext context)
    {
        var type = (INamedTypeSymbol)context.Symbol;
        if (type.TypeKind != TypeKind.Interface)
        {
            return;
        }

        // Property accessors are represented by the property symbol, so counting both would make
        // a clock-only interface look like it had unrelated members.
        var members = type
            .GetMembers()
            .Where(member => member is not IMethodSymbol
            {
                MethodKind: MethodKind.PropertyGet or MethodKind.PropertySet,
            })
            .ToArray();
        if (members.Length == 0)
        {
            return;
        }

        var clockMembers = members.Count(IsClockMember);
        if (clockMembers == 0)
        {
            return;
        }

        var delayMembers = members.Count(IsDelayMember);
        if (members.Length != clockMembers + delayMembers)
        {
            return;
        }

        var described = new List<string>();
        if (clockMembers > 0)
        {
            described.Add($"{clockMembers} clock member(s)");
        }

        if (delayMembers > 0)
        {
            described.Add($"{delayMembers} delay member(s)");
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.DoNotDefineCustomTimeAbstraction,
            type.Locations.FirstOrDefault(),
            type.Name,
            string.Join(" and ", described)));
    }

    private static bool IsClockMember(ISymbol member) => member switch
    {
        IPropertySymbol property =>
            _clockNames.Contains(property.Name) && IsTimeLike(property.Type),
        IMethodSymbol { MethodKind: MethodKind.Ordinary } method =>
            _clockNames.Contains(method.Name) && IsTimeLike(method.ReturnType),
        _ => false,
    };

    private static bool IsDelayMember(ISymbol member) =>
        member is IMethodSymbol { MethodKind: MethodKind.Ordinary } method &&
        _delayNames.Contains(method.Name) &&
        method.Parameters.Any(parameter =>
            parameter.Type.Name == "TimeSpan" &&
            parameter.Type.ContainingNamespace?.Name == "System");

    private static bool IsTimeLike(ITypeSymbol type) =>
        type.SpecialType == SpecialType.System_Int64 ||
        (type.ContainingNamespace?.Name == "System" &&
         type.Name is "DateTime" or "DateTimeOffset");
}
