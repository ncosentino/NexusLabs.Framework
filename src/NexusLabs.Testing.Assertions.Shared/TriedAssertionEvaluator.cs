using NexusLabs.Framework;

namespace NexusLabs.Testing.Assertions;

internal static class TriedAssertionEvaluator
{
    public static TriedAssertionEvaluation<T> Evaluate<T>(TriedEx<T> actual) =>
        actual.Match(
            value => new TriedAssertionEvaluation<T>(
                Success: true,
                Value: value,
                Error: null),
            error => new TriedAssertionEvaluation<T>(
                Success: false,
                Value: default,
                Error: error));

    public static TriedAssertionEvaluation<T?> Evaluate<T>(
        TriedNullEx<T?> actual) =>
        actual.Match(
            value => new TriedAssertionEvaluation<T?>(
                Success: true,
                Value: value,
                Error: null),
            error => new TriedAssertionEvaluation<T?>(
                Success: false,
                Value: default,
                Error: error));
}
