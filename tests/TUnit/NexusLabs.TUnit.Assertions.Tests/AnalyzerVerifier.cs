using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

using NexusLabs.CodeAnalysis.Testing.TUnit;

namespace NexusLabs.TUnit.Assertions.Tests;

internal static class AnalyzerVerifier<TAnalyzer>
    where TAnalyzer : DiagnosticAnalyzer, new()
{
    public static Task VerifyAsync(
        string source,
        CancellationToken cancellationToken) =>
        VerifyAsync(source, [], cancellationToken);

    public static Task VerifyAsync(
        string source,
        DiagnosticResult expected,
        CancellationToken cancellationToken) =>
        VerifyAsync(source, [expected], cancellationToken);

    private static async Task VerifyAsync(
        string source,
        DiagnosticResult[] expected,
        CancellationToken cancellationToken)
    {
        var test = new CSharpAnalyzerTest<TAnalyzer, TUnitVerifier>
        {
            TestCode = source,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
        };

        test.ExpectedDiagnostics.AddRange(expected);
        await test.RunAsync(cancellationToken);
    }
}
