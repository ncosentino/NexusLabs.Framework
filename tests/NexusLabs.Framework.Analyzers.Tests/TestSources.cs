namespace NexusLabs.Framework.Analyzers.Tests;

/// <summary>
/// Reusable C# source snippets injected into analyzer test inputs. The analyzer harness
/// compiles the test source in isolation with only the configured reference assemblies,
/// so any types the test code touches (e.g. <c>TriedEx&lt;T&gt;</c>) have to be visible
/// in the compilation. Defining minimal stubs here keeps the tests self-contained and
/// avoids dragging the real <c>NexusLabs.Framework</c> assembly into the analyzer's
/// in-memory compilation.
/// </summary>
internal static class TestSources
{
    /// <summary>
    /// Minimal <c>TriedEx</c> / <c>TriedNullEx</c> stubs in the real
    /// <c>NexusLabs.Framework</c> namespace. The Try-pattern analyzers gate on this exact
    /// namespace + type name pair, so the stubs must keep both. Append to a test source
    /// (after a trailing newline) so the analyzer's namespace check matches.
    /// </summary>
    public const string TriedExStubs = """

        namespace NexusLabs.Framework
        {
            public readonly struct TriedEx<T>
            {
                public bool Success { get; init; }
                public T Value { get; init; }
                public System.Exception Error { get; init; }
            }

            public readonly struct TriedNullEx<T>
            {
                public bool Success { get; init; }
                public T Value { get; init; }
                public System.Exception Error { get; init; }
            }
        }
        """;
}
