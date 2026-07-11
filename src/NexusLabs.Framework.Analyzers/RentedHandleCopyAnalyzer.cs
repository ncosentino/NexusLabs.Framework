using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace NexusLabs.Framework.Analyzers;

/// <summary>
/// Flags copies of a <c>RentedSpan&lt;T&gt;</c> handle (namespace <c>NexusLabs.Framework.Buffers</c>)
/// — NLF0024. <c>RentedSpan&lt;T&gt;</c> is a single-owner, move-only <c>ref struct</c> whose
/// <c>Dispose</c> returns the rented array to the pool; copying one creates a second owner of the
/// same array, so disposing both copies double-returns it and corrupts the pool.
/// </summary>
/// <remarks>
/// <para>
/// The rule is operation-based rather than syntactic. It targets <c>RentedSpan&lt;T&gt;</c> only:
/// the compiler already forbids every heap escape for a <c>ref struct</c> (boxing, fields, capture,
/// collections, crossing <c>await</c>/<c>yield</c>), so all copies are confined to a single method''s
/// dataflow and are fully analyzable. The reference-type <c>RentedMemory&lt;T&gt;</c> is deliberately
/// NOT analyzed — copying it copies a reference to one shared owner, which is safe by construction.
/// </para>
/// <para>
/// Every read of a handle that materializes a second value is flagged: an assignment source, a
/// variable initializer (including <c>using var b = handle;</c>), a by-value argument, a ternary or
/// switch-expression branch, a tuple element, or returning a <c>using</c>-bound handle. Reads that do
/// not create a second owner are not flagged: the receiver of a member/element access
/// (<c>handle.Span</c>, <c>handle.Dispose()</c>, <c>handle[0]</c>), a <c>ref</c>/<c>in</c>/<c>out</c>
/// argument, an assignment target, a <c>using</c> resource, a <c>nameof</c> operand, an <c>is</c>
/// pattern, a discard, a fresh <c>RentSpan</c> acquisition, and a bare <c>return</c> move of a
/// non-<c>using</c> local. Invoking a non-<c>readonly</c> member (e.g. <c>Dispose()</c>) through an
/// <c>in</c>/<c>ref readonly</c> handle is flagged because the compiler makes a silent defensive copy.
/// </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RentedHandleCopyAnalyzer : DiagnosticAnalyzer
{
    private const string BuffersNamespace = "NexusLabs.Framework.Buffers";
    private const string RentedSpanTypeName = "RentedSpan";

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.DoNotCopyRentedHandle);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterOperationAction(
            AnalyzeReference,
            OperationKind.LocalReference,
            OperationKind.ParameterReference);
    }

    private static void AnalyzeReference(OperationAnalysisContext context)
    {
        var reference = context.Operation;
        if (reference.Type is not INamedTypeSymbol type || !IsRentedSpan(type))
        {
            return;
        }

        var (name, isReadonlyRefContext) = DescribeReference(reference);
        if (name is null)
        {
            return;
        }

        if (!IsCopy(reference, isReadonlyRefContext))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.DoNotCopyRentedHandle,
            reference.Syntax.GetLocation(),
            name,
            type.ToDisplayString()));
    }

    private static (string? Name, bool IsReadonlyRefContext) DescribeReference(IOperation reference) =>
        reference switch
        {
            ILocalReferenceOperation local =>
                (local.Local.Name, local.Local.RefKind == RefKind.RefReadOnly),
            IParameterReferenceOperation parameter =>
                (parameter.Parameter.Name, parameter.Parameter.RefKind == RefKind.In),
            _ => (null, false),
        };

    private static bool IsCopy(IOperation reference, bool isReadonlyRefContext)
    {
        var parent = reference.Parent;
        switch (parent)
        {
            case null:
                return false;

            case IInvocationOperation invocation when ReferenceEquals(invocation.Instance, reference):
                return isReadonlyRefContext && !invocation.TargetMethod.IsReadOnly;

            case IPropertyReferenceOperation property when ReferenceEquals(property.Instance, reference):
                return false;

            case IFieldReferenceOperation field when ReferenceEquals(field.Instance, reference):
                return false;

            case IArgumentOperation argument:
                return argument.Parameter?.RefKind is null or RefKind.None;

            case ISimpleAssignmentOperation assignment when ReferenceEquals(assignment.Target, reference):
                return false;

            case ISimpleAssignmentOperation assignment when assignment.Target is IDiscardOperation:
                return false;

            case IUsingOperation:
            case INameOfOperation:
            case IIsPatternOperation:
            case IDiscardOperation:
                return false;

            case IReturnOperation:
                return reference is ILocalReferenceOperation { Local.IsUsing: true };

            default:
                return true;
        }
    }

    private static bool IsRentedSpan(INamedTypeSymbol type) =>
        type.Name == RentedSpanTypeName
        && type.TypeArguments.Length == 1
        && type.ContainingNamespace?.ToDisplayString() == BuffersNamespace;
}
