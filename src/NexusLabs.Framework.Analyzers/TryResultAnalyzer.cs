using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace NexusLabs.Framework.Analyzers;

/// <summary>
/// Flags reads of <c>TriedEx&lt;T&gt;.Value</c> / <c>TriedNullEx&lt;T&gt;.Value</c> that
/// aren't guarded by a <c>Success</c> check (NLF0002), and reads of
/// <c>.Error</c> that aren't guarded by a negated <c>Success</c> check (NLF0003).
/// Honors short-circuit binary operators (<c>&amp;&amp;</c> / <c>||</c>), conditional
/// (ternary) expressions, and early-return / early-throw / break / continue patterns.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TryResultAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(
            DiagnosticDescriptors.TryResultValueAccessWithoutSuccessCheck,
            DiagnosticDescriptors.TryResultErrorAccessWithoutSuccessCheck);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterOperationAction(AnalyzePropertyReference, OperationKind.PropertyReference);
    }

    private static void AnalyzePropertyReference(OperationAnalysisContext context)
    {
        var propertyReference = (IPropertyReferenceOperation)context.Operation;
        var property = propertyReference.Property;

        if (property?.ContainingType is null)
        {
            return;
        }

        if (!IsTryResultType(property.ContainingType))
        {
            return;
        }

        var propertyName = property.Name;
        if (propertyName is not ("Value" or "Error"))
        {
            return;
        }

        var instance = propertyReference.Instance;
        if (instance is null)
        {
            return;
        }

        var accessLocation = propertyReference.Syntax.GetLocation();
        var isValueProperty = propertyName == "Value";

        if (IsPropertyAccessProtected(context, instance, isValueProperty, accessLocation))
        {
            return;
        }

        var descriptor = isValueProperty
            ? DiagnosticDescriptors.TryResultValueAccessWithoutSuccessCheck
            : DiagnosticDescriptors.TryResultErrorAccessWithoutSuccessCheck;

        context.ReportDiagnostic(Diagnostic.Create(descriptor, accessLocation));
    }

    private static bool IsTryResultType(INamedTypeSymbol type)
    {
        if (type.Name is not ("TriedEx" or "TriedNullEx"))
        {
            return false;
        }

        var ns = type.ContainingNamespace?.ToDisplayString();
        return ns == "NexusLabs.Framework";
    }

    private static bool IsPropertyAccessProtected(
        OperationAnalysisContext context,
        IOperation instance,
        bool isValueProperty,
        Location accessLocation)
    {
        if (IsProtectedByEarlyReturn(context, instance, isValueProperty, accessLocation))
        {
            return true;
        }

        var currentOperation = context.Operation.Parent;
        while (currentOperation is not null)
        {
            if (currentOperation is IBinaryOperation binary &&
                (binary.OperatorKind == BinaryOperatorKind.ConditionalAnd ||
                 binary.OperatorKind == BinaryOperatorKind.ConditionalOr))
            {
                if (IsProtectedByShortCircuitBinaryOperation(binary, instance, isValueProperty, accessLocation))
                {
                    return true;
                }
            }

            if (currentOperation is IConditionalOperation conditional)
            {
                if (IsProtectedByConditional(conditional, instance, isValueProperty, accessLocation))
                {
                    return true;
                }
            }

            currentOperation = currentOperation.Parent;
        }

        return false;
    }

    private static bool IsProtectedByEarlyReturn(
        OperationAnalysisContext context,
        IOperation instance,
        bool isValueProperty,
        Location accessLocation)
    {
        var parentBlocks = new List<IBlockOperation>();
        var currentOperation = context.Operation;

        while (currentOperation is not null)
        {
            if (currentOperation is IBlockOperation block)
            {
                parentBlocks.Add(block);
            }
            currentOperation = currentOperation.Parent;
        }

        foreach (var containingBlock in parentBlocks)
        {
            foreach (var operation in containingBlock.Operations)
            {
                if (operation.Syntax.Span.End > accessLocation.SourceSpan.Start)
                {
                    continue;
                }

                if (operation is not IConditionalOperation conditional ||
                    conditional.WhenFalse is not null)
                {
                    continue;
                }

                if (!ContainsReturn(conditional.WhenTrue))
                {
                    continue;
                }

                if (CheckConditionForSuccess(conditional.Condition, instance, expectTrue: true))
                {
                    if (!isValueProperty)
                    {
                        return true;
                    }
                }

                if (CheckConditionForSuccess(conditional.Condition, instance, expectTrue: false))
                {
                    if (isValueProperty)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private static bool IsProtectedByShortCircuitBinaryOperation(
        IBinaryOperation binary,
        IOperation instance,
        bool isValueProperty,
        Location accessLocation)
    {
        var leftOperand = binary.LeftOperand;
        var rightOperand = binary.RightOperand;

        var accessInRight = IsLocationWithinOperation(accessLocation, rightOperand);
        if (!accessInRight)
        {
            return false;
        }

        if (binary.OperatorKind == BinaryOperatorKind.ConditionalAnd)
        {
            return isValueProperty
                ? CheckConditionForSuccess(leftOperand, instance, expectTrue: true)
                : CheckConditionForSuccess(leftOperand, instance, expectTrue: false);
        }

        if (binary.OperatorKind == BinaryOperatorKind.ConditionalOr)
        {
            return isValueProperty
                ? CheckConditionForSuccess(leftOperand, instance, expectTrue: false)
                : CheckConditionForSuccess(leftOperand, instance, expectTrue: true);
        }

        return false;
    }

    private static bool ContainsReturn(IOperation? operation)
    {
        if (operation is null)
        {
            return false;
        }

        if (operation is IReturnOperation or IThrowOperation or IBranchOperation)
        {
            return true;
        }

        if (operation is IBlockOperation block)
        {
            return block.Operations.Any(ContainsReturn);
        }

        foreach (var child in operation.ChildOperations)
        {
            if (ContainsReturn(child))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsProtectedByConditional(
        IConditionalOperation conditional,
        IOperation instance,
        bool isValueProperty,
        Location accessLocation)
    {
        var condition = conditional.Condition;
        var whenTrue = conditional.WhenTrue;
        var whenFalse = conditional.WhenFalse;

        var accessInTrueBranch = IsLocationWithinOperation(accessLocation, whenTrue);
        var accessInFalseBranch = whenFalse is not null && IsLocationWithinOperation(accessLocation, whenFalse);

        var accessInCondition = IsLocationWithinOperation(accessLocation, condition);
        if (accessInCondition)
        {
            return IsProtectedByShortCircuitEvaluation(condition, instance, isValueProperty, accessLocation);
        }

        if (isValueProperty && accessInTrueBranch)
        {
            return CheckConditionForSuccess(condition, instance, expectTrue: true);
        }
        if (!isValueProperty && accessInFalseBranch)
        {
            return CheckConditionForSuccess(condition, instance, expectTrue: true);
        }
        if (!isValueProperty && accessInTrueBranch)
        {
            return CheckConditionForSuccess(condition, instance, expectTrue: false);
        }
        if (isValueProperty && accessInFalseBranch)
        {
            return CheckConditionForSuccess(condition, instance, expectTrue: false);
        }

        return false;
    }

    private static bool IsProtectedByShortCircuitEvaluation(
        IOperation condition,
        IOperation instance,
        bool isValueProperty,
        Location accessLocation)
    {
        if (condition is not IBinaryOperation binary)
        {
            return false;
        }

        var leftOperand = binary.LeftOperand;
        var rightOperand = binary.RightOperand;

        var accessInLeft = IsLocationWithinOperation(accessLocation, leftOperand);
        var accessInRight = IsLocationWithinOperation(accessLocation, rightOperand);

        if (binary.OperatorKind == BinaryOperatorKind.ConditionalAnd)
        {
            if (accessInRight)
            {
                return isValueProperty
                    ? CheckConditionForSuccess(leftOperand, instance, expectTrue: true)
                    : CheckConditionForSuccess(leftOperand, instance, expectTrue: false);
            }

            if (accessInLeft)
            {
                return IsProtectedByShortCircuitEvaluation(leftOperand, instance, isValueProperty, accessLocation);
            }
        }
        else if (binary.OperatorKind == BinaryOperatorKind.ConditionalOr)
        {
            if (accessInRight)
            {
                return isValueProperty
                    ? CheckConditionForSuccess(leftOperand, instance, expectTrue: false)
                    : CheckConditionForSuccess(leftOperand, instance, expectTrue: true);
            }

            if (accessInLeft)
            {
                return IsProtectedByShortCircuitEvaluation(leftOperand, instance, isValueProperty, accessLocation);
            }
        }

        return false;
    }

    private static bool CheckConditionForSuccess(
        IOperation condition,
        IOperation instance,
        bool expectTrue)
    {
        if (condition is IUnaryOperation unary && unary.OperatorKind == UnaryOperatorKind.Not)
        {
            return CheckConditionForSuccess(unary.Operand, instance, !expectTrue);
        }

        if (condition is IBinaryOperation binary && binary.OperatorKind == BinaryOperatorKind.ConditionalAnd)
        {
            return CheckConditionForSuccess(binary.LeftOperand, instance, expectTrue) ||
                   CheckConditionForSuccess(binary.RightOperand, instance, expectTrue);
        }

        if (condition is IPropertyReferenceOperation propertyRef)
        {
            if (propertyRef.Property.Name == "Success" &&
                IsSameInstance(propertyRef.Instance, instance))
            {
                return expectTrue;
            }
        }

        return false;
    }

    private static bool IsSameInstance(IOperation? op1, IOperation? op2)
    {
        if (op1 is null || op2 is null)
        {
            return false;
        }

        if (op1 is ILocalReferenceOperation local1 && op2 is ILocalReferenceOperation local2)
        {
            return SymbolEqualityComparer.Default.Equals(local1.Local, local2.Local);
        }

        if (op1 is IParameterReferenceOperation param1 && op2 is IParameterReferenceOperation param2)
        {
            return SymbolEqualityComparer.Default.Equals(param1.Parameter, param2.Parameter);
        }

        if (op1 is IMemberReferenceOperation member1 && op2 is IMemberReferenceOperation member2)
        {
            return SymbolEqualityComparer.Default.Equals(member1.Member, member2.Member);
        }

        return false;
    }

    private static bool IsLocationWithinOperation(Location location, IOperation? operation)
    {
        if (operation is null)
        {
            return false;
        }

        return operation.Syntax.Span.Contains(location.SourceSpan);
    }
}
