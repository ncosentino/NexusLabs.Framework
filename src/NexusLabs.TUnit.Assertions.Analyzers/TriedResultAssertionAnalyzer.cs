using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace NexusLabs.TUnit.Assertions.Analyzers;

/// <summary>
/// Reports TUnit <c>Assert.That(...)</c> calls that assert a
/// <c>TriedEx&lt;T&gt;</c> or <c>TriedNullEx&lt;T&gt;</c> member instead of
/// asserting the complete result with <c>Succeeded()</c> or <c>Failed()</c>.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TriedResultAssertionAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.AssertTriedResultDirectly);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context)
    {
        var invocation = (IInvocationOperation)context.Operation;
        if (!IsTUnitAssertThat(invocation.TargetMethod))
        {
            return;
        }

        var assertedValue = invocation.Arguments
            .FirstOrDefault(argument => argument.Parameter?.Ordinal == 0)
            ?.Value;
        assertedValue = UnwrapConversion(assertedValue);

        if (assertedValue is not IPropertyReferenceOperation propertyReference ||
            !IsTriedResultProperty(propertyReference.Property))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.AssertTriedResultDirectly,
            propertyReference.Syntax.GetLocation(),
            propertyReference.Property.Name));
    }

    private static bool IsTUnitAssertThat(IMethodSymbol method)
    {
        if (method.Name != "That" ||
            method.ContainingType?.Name != "Assert")
        {
            return false;
        }

        return method.ContainingNamespace?.ToDisplayString() == "TUnit.Assertions";
    }

    private static bool IsTriedResultProperty(IPropertySymbol property)
    {
        if (property.Name is not ("Success" or "Value" or "Error"))
        {
            return false;
        }

        var containingType = property.ContainingType;
        if (containingType.Name is not ("TriedEx" or "TriedNullEx"))
        {
            return false;
        }

        return containingType.ContainingNamespace?.ToDisplayString() ==
            "NexusLabs.Framework";
    }

    private static IOperation? UnwrapConversion(IOperation? operation)
    {
        while (operation is IConversionOperation conversion)
        {
            operation = conversion.Operand;
        }

        return operation;
    }
}
