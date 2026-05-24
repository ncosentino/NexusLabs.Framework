using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace NexusLabs.Framework.Analyzers.Tests;

/// <summary>
/// Thin convenience wrapper around <see cref="CSharpAnalyzerTest{TAnalyzer, TVerifier}"/>
/// configured for the conventions of this repo. Tests pass the source under analysis as
/// a string and an optional set of expected diagnostics; the verifier compares the analyzer's
/// reported diagnostics against the expectations and fails the test on mismatch.
/// </summary>
internal static class AnalyzerVerifier<TAnalyzer>
    where TAnalyzer : DiagnosticAnalyzer, new()
{
    public static async Task VerifyAnalyzerAsync(
        string source,
        params DiagnosticResult[] expected)
    {
        var test = new CSharpAnalyzerTest<TAnalyzer, DefaultVerifier>
        {
            TestCode = source,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
        };

        test.ExpectedDiagnostics.AddRange(expected);

        await test.RunAsync();
    }

    public static DiagnosticResult Diagnostic(DiagnosticDescriptor descriptor) =>
        new(descriptor);
}
