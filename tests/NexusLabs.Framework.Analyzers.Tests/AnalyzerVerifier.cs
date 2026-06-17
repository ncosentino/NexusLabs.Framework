using System.Collections.Generic;
using System.Threading;
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
    public static Task VerifyAnalyzerAsync(
        string source,
        CancellationToken cancellationToken)
        => VerifyAnalyzerAsync(source, [], cancellationToken);

    public static Task VerifyAnalyzerAsync(
        string source,
        DiagnosticResult expected,
        CancellationToken cancellationToken)
        => VerifyAnalyzerAsync(source, [expected], cancellationToken);

    public static async Task VerifyAnalyzerAsync(
        string source,
        DiagnosticResult[] expected,
        CancellationToken cancellationToken)
    {
        var test = new CSharpAnalyzerTest<TAnalyzer, DefaultVerifier>
        {
            TestCode = source,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
        };

        test.ExpectedDiagnostics.AddRange(expected);

        await test.RunAsync(cancellationToken);
    }

    /// <summary>
    /// Variant that pre-compiles a single additional project and references it
    /// from the primary test compilation. Use this to exercise behavior that
    /// depends on a type being loaded from metadata (a separate referenced
    /// assembly) rather than from source — for example, analyzers that branch
    /// on <see cref="Location.IsInMetadata"/>.
    /// </summary>
    public static Task VerifyAnalyzerWithAdditionalProjectAsync(
        string source,
        string additionalProjectName,
        IEnumerable<string> additionalProjectSources,
        CancellationToken cancellationToken)
        => VerifyAnalyzerWithAdditionalProjectAsync(
            source,
            additionalProjectName,
            additionalProjectSources,
            [],
            cancellationToken);

    public static Task VerifyAnalyzerWithAdditionalProjectAsync(
        string source,
        string additionalProjectName,
        IEnumerable<string> additionalProjectSources,
        DiagnosticResult expected,
        CancellationToken cancellationToken)
        => VerifyAnalyzerWithAdditionalProjectAsync(
            source,
            additionalProjectName,
            additionalProjectSources,
            [expected],
            cancellationToken);

    public static async Task VerifyAnalyzerWithAdditionalProjectAsync(
        string source,
        string additionalProjectName,
        IEnumerable<string> additionalProjectSources,
        DiagnosticResult[] expected,
        CancellationToken cancellationToken)
    {
        var test = new CSharpAnalyzerTest<TAnalyzer, DefaultVerifier>
        {
            TestCode = source,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
        };

        var additionalProject = new ProjectState(
            additionalProjectName,
            LanguageNames.CSharp,
            defaultPrefix: $"{additionalProjectName}_",
            defaultExtension: "cs");

        var fileIndex = 0;
        foreach (var src in additionalProjectSources)
        {
            additionalProject.Sources.Add(($"{additionalProjectName}_{fileIndex}.cs", src));
            fileIndex++;
        }

        test.TestState.AdditionalProjects.Add(additionalProjectName, additionalProject);
        test.TestState.AdditionalProjectReferences.Add(additionalProjectName);

        test.ExpectedDiagnostics.AddRange(expected);

        await test.RunAsync(cancellationToken);
    }

    public static DiagnosticResult Diagnostic(DiagnosticDescriptor descriptor) =>
        new(descriptor);
}
