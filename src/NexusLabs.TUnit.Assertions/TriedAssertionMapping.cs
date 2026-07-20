using System;

using NexusLabs.Framework;
using NexusLabs.Testing.Assertions;

namespace NexusLabs.TUnit.Assertions;

internal static class TriedAssertionMapping
{
    public static T GetSuccessfulValue<T>(TriedEx<T> actual)
    {
        var evaluation = TriedAssertionEvaluator.Evaluate(actual);
        if (evaluation.Success)
        {
            return evaluation.Value!;
        }

        var error = evaluation.Error!;
        throw new TriedAssertionEvaluationException(
            $"the result failed with {error.GetType().Name}: {error.Message}",
            error);
    }

    public static T? GetSuccessfulValue<T>(TriedNullEx<T?> actual)
    {
        var evaluation = TriedAssertionEvaluator.Evaluate(actual);
        if (evaluation.Success)
        {
            return evaluation.Value;
        }

        var error = evaluation.Error!;
        throw new TriedAssertionEvaluationException(
            $"the result failed with {error.GetType().Name}: {error.Message}",
            error);
    }

    public static Exception GetFailure<T>(TriedEx<T> actual)
    {
        var evaluation = TriedAssertionEvaluator.Evaluate(actual);
        if (!evaluation.Success)
        {
            return evaluation.Error!;
        }

        throw new TriedAssertionEvaluationException(
            "the result succeeded",
            actualError: null);
    }

    public static Exception GetFailure<T>(TriedNullEx<T?> actual)
    {
        var evaluation = TriedAssertionEvaluator.Evaluate(actual);
        if (!evaluation.Success)
        {
            return evaluation.Error!;
        }

        throw new TriedAssertionEvaluationException(
            "the result succeeded",
            actualError: null);
    }

    public static TException GetFailure<TException>(Exception? error)
        where TException : Exception
    {
        if (error is null)
        {
            throw new TriedAssertionEvaluationException(
                "the result did not contain an exception",
                actualError: null);
        }

        if (error is TException typedError)
        {
            return typedError;
        }

        throw new TriedAssertionEvaluationException(
            $"the result failed with {error.GetType().Name} instead of {typeof(TException).Name}",
            error);
    }
}
