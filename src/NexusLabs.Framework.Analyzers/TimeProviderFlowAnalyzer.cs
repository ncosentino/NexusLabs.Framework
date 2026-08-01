using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace NexusLabs.Framework.Analyzers;

/// <summary>
/// Flags a call or construction that has a <c>System.TimeProvider</c> overload but does not use
/// it. Covers both shapes of the same mistake:
/// <list type="bullet">
///   <item>invocations such as <c>Task.Delay(delay, cancellationToken)</c></item>
///   <item>constructions such as <c>new CancellationTokenSource(delay)</c> and
///         <c>new PeriodicTimer(period)</c></item>
/// </list>
/// Reports NLF0025 when a clock is reachable from the call site and NLF0026 when one is not, so
/// a single call site is never reported by both rules.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TimeProviderFlowAnalyzer : DiagnosticAnalyzer
{
    private const string TimeProviderMetadataName = "System.TimeProvider";

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(
            DiagnosticDescriptors.ForwardAvailableTimeProvider,
            DiagnosticDescriptors.TimeProviderOverloadAvailable);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(compilationContext =>
        {
            var timeProvider = compilationContext.Compilation
                .GetTypeByMetadataName(TimeProviderMetadataName);
            if (timeProvider is null)
            {
                return;
            }

            compilationContext.RegisterOperationAction(
                operationContext => AnalyzeInvocation(operationContext, timeProvider),
                OperationKind.Invocation);

            compilationContext.RegisterOperationAction(
                operationContext => AnalyzeObjectCreation(operationContext, timeProvider),
                OperationKind.ObjectCreation);
        });
    }

    private static void AnalyzeInvocation(
        OperationAnalysisContext context,
        INamedTypeSymbol timeProvider)
    {
        var operation = (IInvocationOperation)context.Operation;
        var target = operation.TargetMethod;
        if (AcceptsTimeProvider(target, timeProvider))
        {
            return;
        }

        var overloads = target.ContainingType.GetMembers(target.Name).OfType<IMethodSymbol>();
        if (!HasTimeProviderOverload(overloads, target, timeProvider))
        {
            return;
        }

        Report(
            context,
            operation,
            $"{target.ContainingType.Name}.{target.Name}",
            timeProvider);
    }

    private static void AnalyzeObjectCreation(
        OperationAnalysisContext context,
        INamedTypeSymbol timeProvider)
    {
        var operation = (IObjectCreationOperation)context.Operation;
        var constructor = operation.Constructor;
        if (constructor is null || AcceptsTimeProvider(constructor, timeProvider))
        {
            return;
        }

        var overloads = constructor.ContainingType.InstanceConstructors;
        if (!HasTimeProviderOverload(overloads, constructor, timeProvider))
        {
            return;
        }

        Report(context, operation, constructor.ContainingType.Name, timeProvider);
    }

    private static void Report(
        OperationAnalysisContext context,
        IOperation operation,
        string target,
        INamedTypeSymbol timeProvider)
    {
        var available = FindReachableClocks(context.ContainingSymbol, timeProvider);
        var location = operation.Syntax.GetLocation();

        context.ReportDiagnostic(available.Count > 0
            ? Diagnostic.Create(
                DiagnosticDescriptors.ForwardAvailableTimeProvider,
                location,
                target,
                string.Join(", ", available))
            : Diagnostic.Create(
                DiagnosticDescriptors.TimeProviderOverloadAvailable,
                location,
                target));
    }

    private static bool AcceptsTimeProvider(IMethodSymbol method, INamedTypeSymbol timeProvider) =>
        method.Parameters.Any(parameter => IsTimeProvider(parameter.Type, timeProvider));

    /// <summary>
    /// An overload only counts when it takes strictly more parameters than the resolved target.
    /// A shorter or equal-length overload is a different operation rather than the clock-aware
    /// form of the same one.
    /// </summary>
    private static bool HasTimeProviderOverload(
        IEnumerable<IMethodSymbol> candidates,
        IMethodSymbol current,
        INamedTypeSymbol timeProvider) =>
        candidates.Any(candidate =>
            !SymbolEqualityComparer.Default.Equals(candidate, current) &&
            candidate.Parameters.Length > current.Parameters.Length &&
            AcceptsTimeProvider(candidate, timeProvider));

    private static bool IsTimeProvider(ITypeSymbol type, INamedTypeSymbol timeProvider) =>
        SymbolEqualityComparer.Default.Equals(type, timeProvider);

    private static List<string> FindReachableClocks(
        ISymbol? containingSymbol,
        INamedTypeSymbol timeProvider)
    {
        var found = new List<string>();
        if (containingSymbol is null)
        {
            return found;
        }

        if (containingSymbol is IMethodSymbol method)
        {
            foreach (var parameter in method.Parameters)
            {
                if (IsTimeProvider(parameter.Type, timeProvider))
                {
                    found.Add(parameter.Name);
                }
            }
        }

        for (var type = containingSymbol.ContainingType; type is not null; type = type.BaseType)
        {
            foreach (var member in type.GetMembers())
            {
                // Statics are excluded deliberately. Every type deriving from TimeProvider
                // inherits the static TimeProvider.System, and reporting that as "a clock is
                // already available" would be advice to bind to the machine clock.
                if (member.IsStatic)
                {
                    continue;
                }

                switch (member)
                {
                    case IFieldSymbol field when IsTimeProvider(field.Type, timeProvider):
                        found.Add(field.Name);
                        break;
                    case IPropertySymbol property when IsTimeProvider(property.Type, timeProvider):
                        found.Add(property.Name);
                        break;
                    default:
                        break;
                }
            }

            // Primary constructor parameters are not members, so the constructor list is the only
            // place a captured clock shows up.
            foreach (var constructor in type.InstanceConstructors)
            {
                foreach (var parameter in constructor.Parameters)
                {
                    if (IsTimeProvider(parameter.Type, timeProvider))
                    {
                        found.Add(parameter.Name);
                    }
                }
            }
        }

        return found.Distinct().ToList();
    }
}
