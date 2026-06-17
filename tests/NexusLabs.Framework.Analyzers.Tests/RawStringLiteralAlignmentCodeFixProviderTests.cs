using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

using Xunit;

namespace NexusLabs.Framework.Analyzers.Tests;

public sealed class RawStringLiteralAlignmentCodeFixProviderTests
{
    [Fact]
    public async Task DanglingOnAssignment_Fix_MovesOpeningToOwnLine()
    {
        var source =
            """""
            class C
            {
                void M()
                {
                    var s = {|#0:"""|}
                        hello
                        """;
                }
            }
            """"";

        var fixedSource =
            """""
            class C
            {
                void M()
                {
                    var s =
                        """
                        hello
                        """;
                }
            }
            """"";

        var expected = new DiagnosticResult("NLF0010", DiagnosticSeverity.Warning).WithLocation(0);
        await VerifyCodeFixAsync(source, fixedSource, expected, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task DanglingOnReturn_Fix_MovesOpeningToOwnLine()
    {
        var source =
            """""
            class C
            {
                string M()
                {
                    return {|#0:"""|}
                        hello
                        """;
                }
            }
            """"";

        var fixedSource =
            """""
            class C
            {
                string M()
                {
                    return
                        """
                        hello
                        """;
                }
            }
            """"";

        var expected = new DiagnosticResult("NLF0010", DiagnosticSeverity.Warning).WithLocation(0);
        await VerifyCodeFixAsync(source, fixedSource, expected, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task DanglingInMethodArgument_Fix_MovesOpeningToOwnLine()
    {
        var source =
            """""
            class C
            {
                void M()
                {
                    System.Console.WriteLine({|#0:"""|}
                        hello
                        """);
                }
            }
            """"";

        var fixedSource =
            """""
            class C
            {
                void M()
                {
                    System.Console.WriteLine(
                        """
                        hello
                        """);
                }
            }
            """"";

        var expected = new DiagnosticResult("NLF0010", DiagnosticSeverity.Warning).WithLocation(0);
        await VerifyCodeFixAsync(source, fixedSource, expected, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task DanglingInterpolated_Fix_MovesOpeningToOwnLine()
    {
        var source =
            """""
            class C
            {
                void M(string name)
                {
                    var s = {|#0:$"""|}
                        hello {name}
                        """;
                }
            }
            """"";

        var fixedSource =
            """""
            class C
            {
                void M(string name)
                {
                    var s =
                        $"""
                        hello {name}
                        """;
                }
            }
            """"";

        var expected = new DiagnosticResult("NLF0010", DiagnosticSeverity.Warning).WithLocation(0);
        await VerifyCodeFixAsync(source, fixedSource, expected, TestContext.Current.CancellationToken);
    }

    private static Task VerifyCodeFixAsync(
        string source,
        string fixedSource,
        DiagnosticResult expected,
        CancellationToken cancellationToken)
        => VerifyCodeFixAsync(source, fixedSource, [expected], cancellationToken);

    private static async Task VerifyCodeFixAsync(
        string source,
        string fixedSource,
        DiagnosticResult[] expected,
        CancellationToken cancellationToken)
    {
        var test = new CSharpCodeFixTest<RawStringLiteralAlignmentAnalyzer, RawStringLiteralAlignmentCodeFixProvider, DefaultVerifier>
        {
            TestCode = source,
            FixedCode = fixedSource,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
        };

        test.ExpectedDiagnostics.AddRange(expected);

        await test.RunAsync(cancellationToken);
    }
}
