using System;
using System.Threading.Tasks;

using TUnit.Assertions.Core;

namespace NexusLabs.TUnit.Assertions;

internal static class TriedAssertionResult
{
    public static Task<AssertionResult> FromException(Exception? exception)
    {
        if (exception is null)
        {
            return Task.FromResult(AssertionResult.Passed);
        }

        if (exception is TriedAssertionEvaluationException evaluationException)
        {
            return Task.FromResult(AssertionResult.Failed(
                evaluationException.Message,
                evaluationException.ActualError));
        }

        return Task.FromResult(AssertionResult.Failed(
            $"evaluating the asserted result threw {exception.GetType().Name}: {exception.Message}",
            exception));
    }
}
