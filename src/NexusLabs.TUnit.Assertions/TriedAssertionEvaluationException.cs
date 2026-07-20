using System;

namespace NexusLabs.TUnit.Assertions;

internal sealed class TriedAssertionEvaluationException : Exception
{
    public TriedAssertionEvaluationException()
    {
    }

    public TriedAssertionEvaluationException(string message)
        : base(message)
    {
    }

    public TriedAssertionEvaluationException(
        string message,
        Exception? actualError)
        : base(message, actualError)
    {
        ActualError = actualError;
    }

    public Exception? ActualError { get; }
}
