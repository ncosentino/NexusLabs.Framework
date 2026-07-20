using System;

namespace NexusLabs.Testing.Assertions;

internal readonly record struct TriedAssertionEvaluation<T>(
    bool Success,
    T? Value,
    Exception? Error);
