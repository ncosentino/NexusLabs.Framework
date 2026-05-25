using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace NexusLabs.Framework.Analyzers;

/// <summary>
/// Two diagnostics over <c>NexusLabs.Framework.TriedEx&lt;T&gt;</c> /
/// <c>TriedNullEx&lt;T&gt;</c> error usage:
/// <list type="bullet">
///   <item><c>NLF0004</c>: redundant null check on <c>.Error</c> after the
///         caller has already established that <c>Success</c> is false (the
///         contract guarantees <c>Error</c> is non-null in that case).</item>
///   <item><c>NLF0005</c>: returning an exception from a failure branch
///         without preserving the original <c>.Error</c> (return it directly,
///         wrap it as inner, or include it in an aggregate — anything else
///         silently drops the original error.</item>
/// </list>
/// Both checks are namespace-gated to <c>NexusLabs.Framework</c>.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TryResultErrorUsageAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(
            DiagnosticDescriptors.TryResultErrorNullCheckAfterSuccessCheck,
            DiagnosticDescriptors.TryResultErrorMustBePreserved);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterOperationAction(AnalyzePropertyReference, OperationKind.PropertyReference);
        context.RegisterOperationAction(AnalyzeReturnStatement, OperationKind.Return);
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

        if (property.Name != "Error")
        {
            return;
        }

        var currentOperation = context.Operation.Parent;
        IOperation? nullCheckOperation = null;
        while (currentOperation is not null)
        {
            if (currentOperation is IConversionOperation)
            {
                currentOperation = currentOperation.Parent;
                continue;
            }

            if (IsNullCheckPattern(currentOperation))
            {
                nullCheckOperation = currentOperation;
                break;
            }

            break;
        }

        if (nullCheckOperation is null)
        {
            return;
        }

        var instance = propertyReference.Instance;
        if (instance is null)
        {
            return;
        }

        if (IsErrorAccessProtectedByFailureCheck(context, instance, propertyReference.Syntax.GetLocation()))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.TryResultErrorNullCheckAfterSuccessCheck,
                propertyReference.Syntax.GetLocation()));
        }
    }

    private static void AnalyzeReturnStatement(OperationAnalysisContext context)
    {
        var returnOperation = (IReturnOperation)context.Operation;

        if (returnOperation.ReturnedValue is null)
        {
            return;
        }

        var returnType = returnOperation.ReturnedValue.Type;
        if (returnType is null || !IsExceptionType(returnType))
        {
            return;
        }

        var (isInFailureBranch, tryResultInstance) = IsInSuccessFailureBranch(returnOperation);
        if (!isInFailureBranch || tryResultInstance is null)
        {
            return;
        }

        var returnedValue = returnOperation.ReturnedValue;

        if (IsErrorPropertyAccess(returnedValue, tryResultInstance))
        {
            return;
        }

        if (IsNewExceptionWithErrorAsInner(returnedValue, tryResultInstance))
        {
            return;
        }

        if (ContainsAnyErrorPropertyAccess(returnedValue))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.TryResultErrorMustBePreserved,
            returnOperation.Syntax.GetLocation()));
    }

    private static bool IsNullCheckPattern(IOperation operation)
    {
        if (operation is IBinaryOperation binaryOp)
        {
            if (binaryOp.OperatorKind is BinaryOperatorKind.Equals or BinaryOperatorKind.NotEquals)
            {
                return IsNullLiteral(binaryOp.LeftOperand) || IsNullLiteral(binaryOp.RightOperand);
            }

            return false;
        }

        if (operation is IIsPatternOperation isPattern)
        {
            return IsNullPatternCheck(isPattern.Pattern);
        }

        if (operation is IConditionalAccessOperation)
        {
            return true;
        }

        return false;
    }

    private static bool IsNullLiteral(IOperation? operation)
    {
        if (operation is null)
        {
            return false;
        }

        while (operation is IConversionOperation conversion)
        {
            operation = conversion.Operand;
        }

        if (operation is ILiteralOperation literal &&
            literal.ConstantValue.HasValue &&
            literal.ConstantValue.Value is null)
        {
            return true;
        }

        return operation is IDefaultValueOperation;
    }

    private static bool IsNullPatternCheck(IPatternOperation? pattern)
    {
        if (pattern is null)
        {
            return false;
        }

        if (pattern is IConstantPatternOperation constantPattern)
        {
            return IsNullLiteral(constantPattern.Value);
        }

        if (pattern is INegatedPatternOperation negatedPattern)
        {
            return IsNullPatternCheck(negatedPattern.Pattern);
        }

        return false;
    }

    private static bool IsErrorAccessProtectedByFailureCheck(
        OperationAnalysisContext context,
        IOperation instance,
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

                if (operation is not IConditionalOperation conditional || conditional.WhenFalse is not null)
                {
                    continue;
                }

                if (!ContainsReturn(conditional.WhenTrue))
                {
                    continue;
                }

                if (CheckConditionForSuccess(conditional.Condition, instance, expectTrue: true))
                {
                    return true;
                }
            }
        }

        currentOperation = context.Operation.Parent;
        while (currentOperation is not null)
        {
            if (currentOperation is IConditionalOperation conditional)
            {
                var accessInTrueBranch = IsLocationWithinOperation(accessLocation, conditional.WhenTrue);
                var accessInFalseBranch = conditional.WhenFalse is not null && IsLocationWithinOperation(accessLocation, conditional.WhenFalse);

                if (accessInFalseBranch && CheckConditionForSuccess(conditional.Condition, instance, expectTrue: true))
                {
                    return true;
                }

                if (accessInTrueBranch && CheckConditionForSuccess(conditional.Condition, instance, expectTrue: false))
                {
                    return true;
                }
            }

            currentOperation = currentOperation.Parent;
        }

        return false;
    }

    private static (bool isInFailureBranch, IOperation? instance) IsInSuccessFailureBranch(
        IReturnOperation returnOperation)
    {
        var parentBlocks = new List<IBlockOperation>();
        var currentOperation = (IOperation)returnOperation;
        while (currentOperation is not null)
        {
            if (currentOperation is IBlockOperation block)
            {
                parentBlocks.Add(block);
            }
            currentOperation = currentOperation.Parent!;
        }

        foreach (var containingBlock in parentBlocks)
        {
            foreach (var operation in containingBlock.Operations)
            {
                if (operation.Syntax.Span.End > returnOperation.Syntax.GetLocation().SourceSpan.Start)
                {
                    continue;
                }

                if (operation is not IConditionalOperation conditional || conditional.WhenFalse is not null)
                {
                    continue;
                }

                if (!ContainsReturn(conditional.WhenTrue))
                {
                    continue;
                }

                var instance = FindSuccessCheckInstance(conditional.Condition);
                if (instance is not null && CheckConditionForSuccess(conditional.Condition, instance, expectTrue: true))
                {
                    return (true, instance);
                }
            }
        }

        currentOperation = returnOperation.Parent;
        while (currentOperation is not null)
        {
            if (currentOperation is IConditionalOperation conditional)
            {
                var location = returnOperation.Syntax.GetLocation();
                var accessInTrueBranch = IsLocationWithinOperation(location, conditional.WhenTrue);
                var accessInFalseBranch = conditional.WhenFalse is not null && IsLocationWithinOperation(location, conditional.WhenFalse);

                var instance = FindSuccessCheckInstance(conditional.Condition);
                if (instance is null)
                {
                    currentOperation = currentOperation.Parent;
                    continue;
                }

                if (accessInFalseBranch && CheckConditionForSuccess(conditional.Condition, instance, expectTrue: true))
                {
                    return (true, instance);
                }

                if (accessInTrueBranch && CheckConditionForSuccess(conditional.Condition, instance, expectTrue: false))
                {
                    return (true, instance);
                }
            }

            currentOperation = currentOperation.Parent;
        }

        return (false, null);
    }

    private static IOperation? FindSuccessCheckInstance(IOperation condition)
    {
        if (condition is IUnaryOperation unary && unary.OperatorKind == UnaryOperatorKind.Not)
        {
            return FindSuccessCheckInstance(unary.Operand);
        }

        if (condition is IPropertyReferenceOperation propertyRef &&
            propertyRef.Property.Name == "Success" &&
            IsTryResultType(propertyRef.Property.ContainingType))
        {
            return propertyRef.Instance;
        }

        if (condition is IBinaryOperation binary && binary.OperatorKind == BinaryOperatorKind.ConditionalAnd)
        {
            return FindSuccessCheckInstance(binary.LeftOperand) ?? FindSuccessCheckInstance(binary.RightOperand);
        }

        return null;
    }

    private static bool IsErrorPropertyAccess(IOperation operation, IOperation expectedInstance)
    {
        if (operation is IPropertyReferenceOperation propertyRef &&
            propertyRef.Property.Name == "Error" &&
            IsTryResultType(propertyRef.Property.ContainingType) &&
            IsSameInstance(propertyRef.Instance, expectedInstance))
        {
            return true;
        }

        if (operation is IConversionOperation conversion)
        {
            return IsErrorPropertyAccess(conversion.Operand, expectedInstance);
        }

        return false;
    }

    private static bool IsNewExceptionWithErrorAsInner(IOperation operation, IOperation expectedInstance)
    {
        if (operation is IConversionOperation conversion)
        {
            return IsNewExceptionWithErrorAsInner(conversion.Operand, expectedInstance);
        }

        if (operation is IObjectCreationOperation objectCreation &&
            objectCreation.Type is not null &&
            IsExceptionType(objectCreation.Type))
        {
            foreach (var argument in objectCreation.Arguments)
            {
                if (ContainsErrorPropertyAccess(argument.Value, expectedInstance))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool ContainsErrorPropertyAccess(IOperation? operation, IOperation expectedInstance)
    {
        if (operation is null)
        {
            return false;
        }

        if (IsErrorPropertyAccess(operation, expectedInstance))
        {
            return true;
        }

        if (operation is IConversionOperation conversion)
        {
            return ContainsErrorPropertyAccess(conversion.Operand, expectedInstance);
        }

        if (operation is IArrayCreationOperation arrayCreation && arrayCreation.Initializer is not null)
        {
            foreach (var element in arrayCreation.Initializer.ElementValues)
            {
                if (ContainsErrorPropertyAccess(element, expectedInstance))
                {
                    return true;
                }
            }
        }

        if (operation is ICollectionExpressionOperation collectionExpr)
        {
            foreach (var element in collectionExpr.Elements)
            {
                if (ContainsErrorPropertyAccess(element, expectedInstance))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool ContainsAnyErrorPropertyAccess(IOperation? operation)
    {
        if (operation is null)
        {
            return false;
        }

        if (operation is IPropertyReferenceOperation propRef &&
            propRef.Property.Name == "Error" &&
            IsTryResultType(propRef.Property.ContainingType))
        {
            return true;
        }

        if (operation is IConversionOperation conversion)
        {
            return ContainsAnyErrorPropertyAccess(conversion.Operand);
        }

        if (operation is IConditionalOperation conditional)
        {
            return ContainsAnyErrorPropertyAccess(conditional.WhenTrue) ||
                   (conditional.WhenFalse is not null && ContainsAnyErrorPropertyAccess(conditional.WhenFalse));
        }

        if (operation is ICoalesceOperation coalesce)
        {
            return ContainsAnyErrorPropertyAccess(coalesce.Value) ||
                   ContainsAnyErrorPropertyAccess(coalesce.WhenNull);
        }

        return false;
    }

    private static bool IsExceptionType(ITypeSymbol type)
    {
        if (type.Name == "Exception" && type.ContainingNamespace?.ToDisplayString() == "System")
        {
            return true;
        }

        var baseType = type.BaseType;
        while (baseType is not null)
        {
            if (baseType.Name == "Exception" && baseType.ContainingNamespace?.ToDisplayString() == "System")
            {
                return true;
            }
            baseType = baseType.BaseType;
        }

        return false;
    }

    private static bool IsTryResultType(ITypeSymbol? type)
    {
        if (type is null)
        {
            return false;
        }

        return type.Name is "TriedEx" or "TriedNullEx" &&
               type.ContainingNamespace?.ToDisplayString() == "NexusLabs.Framework";
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

        if (condition is IPropertyReferenceOperation propertyRef &&
            propertyRef.Property.Name == "Success" &&
            IsSameInstance(propertyRef.Instance, instance))
        {
            return expectTrue;
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
