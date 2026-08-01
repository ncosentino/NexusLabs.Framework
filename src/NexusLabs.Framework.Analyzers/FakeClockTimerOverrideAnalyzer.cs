using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace NexusLabs.Framework.Analyzers;

/// <summary>
/// Flags a fake time provider that replaces timer creation. Such an override substitutes the
/// virtual-time scheduler for everything the test drives through that clock, so delays complete
/// without the clock ever moving and the behaviour under test is never exercised.
/// </summary>
/// <remarks>
/// An override that calls <c>base.CreateTimer</c> is not reported. Delegating to the base keeps
/// the scheduler in play, which is what makes registration-observing wrappers legitimate.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class FakeClockTimerOverrideAnalyzer : DiagnosticAnalyzer
{
    private const string FakeTimeProviderMetadataName =
        "Microsoft.Extensions.Time.Testing.FakeTimeProvider";

    private const string CreateTimerMethodName = "CreateTimer";

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.DoNotOverrideFakeClockCreateTimer);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(compilationContext =>
        {
            var fakeTimeProvider = compilationContext.Compilation
                .GetTypeByMetadataName(FakeTimeProviderMetadataName);
            if (fakeTimeProvider is null)
            {
                return;
            }

            compilationContext.RegisterOperationBlockAction(
                blockContext => Analyze(blockContext, fakeTimeProvider));
        });
    }

    private static void Analyze(
        OperationBlockAnalysisContext context,
        INamedTypeSymbol fakeTimeProvider)
    {
        if (context.OwningSymbol is not IMethodSymbol
            {
                IsOverride: true,
                Name: CreateTimerMethodName,
            } method)
        {
            return;
        }

        var type = method.ContainingType;
        if (type.TypeKind != TypeKind.Class || !DerivesFrom(type, fakeTimeProvider))
        {
            return;
        }

        if (context.OperationBlocks.Any(DelegatesToBase))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.DoNotOverrideFakeClockCreateTimer,
            method.Locations.FirstOrDefault() ?? type.Locations.FirstOrDefault(),
            type.Name));
    }

    private static bool DelegatesToBase(IOperation block) =>
        block
            .DescendantsAndSelf()
            .OfType<IInvocationOperation>()
            .Any(invocation =>
                invocation.TargetMethod.Name == CreateTimerMethodName &&
                invocation.Syntax is InvocationExpressionSyntax
                {
                    Expression: MemberAccessExpressionSyntax { Expression: BaseExpressionSyntax },
                });

    private static bool DerivesFrom(INamedTypeSymbol type, INamedTypeSymbol target)
    {
        for (var current = type.BaseType; current is not null; current = current.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(current, target))
            {
                return true;
            }
        }

        return false;
    }
}
