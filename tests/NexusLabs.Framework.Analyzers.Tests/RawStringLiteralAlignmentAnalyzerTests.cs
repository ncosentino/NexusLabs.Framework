using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

using Xunit;

namespace NexusLabs.Framework.Analyzers.Tests;

public sealed class RawStringLiteralAlignmentAnalyzerTests
{
    [Fact]
    public async Task DanglingOpening_OnAssignment_Reports()
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

        var expected = AnalyzerVerifier<RawStringLiteralAlignmentAnalyzer>
            .Diagnostic(DiagnosticDescriptors.RawStringOpeningQuotesMustBeOnOwnLine)
            .WithLocation(0);

        await AnalyzerVerifier<RawStringLiteralAlignmentAnalyzer>.VerifyAnalyzerAsync(source, expected, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task AlignedOpening_OnAssignment_NoDiagnostic()
    {
        var source =
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

        await AnalyzerVerifier<RawStringLiteralAlignmentAnalyzer>.VerifyAnalyzerAsync(source, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task SingleLineRawString_NoDiagnostic()
    {
        var source =
            """""
            class C
            {
                void M()
                {
                    var s = """value""";
                }
            }
            """"";

        await AnalyzerVerifier<RawStringLiteralAlignmentAnalyzer>.VerifyAnalyzerAsync(source, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task DanglingOpening_InMethodArgument_Reports()
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

        var expected = AnalyzerVerifier<RawStringLiteralAlignmentAnalyzer>
            .Diagnostic(DiagnosticDescriptors.RawStringOpeningQuotesMustBeOnOwnLine)
            .WithLocation(0);

        await AnalyzerVerifier<RawStringLiteralAlignmentAnalyzer>.VerifyAnalyzerAsync(source, expected, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task AlignedOpening_InMethodArgument_NoDiagnostic()
    {
        var source =
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

        await AnalyzerVerifier<RawStringLiteralAlignmentAnalyzer>.VerifyAnalyzerAsync(source, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task DanglingOpening_OnReturn_Reports()
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

        var expected = AnalyzerVerifier<RawStringLiteralAlignmentAnalyzer>
            .Diagnostic(DiagnosticDescriptors.RawStringOpeningQuotesMustBeOnOwnLine)
            .WithLocation(0);

        await AnalyzerVerifier<RawStringLiteralAlignmentAnalyzer>.VerifyAnalyzerAsync(source, expected, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task DanglingOpening_InAttribute_Reports()
    {
        var source =
            """""
            class MyAttr : System.Attribute
            {
                public MyAttr(string s) { }
            }

            [MyAttr({|#0:"""|}
                hello
                """)]
            class C { }
            """"";

        var expected = AnalyzerVerifier<RawStringLiteralAlignmentAnalyzer>
            .Diagnostic(DiagnosticDescriptors.RawStringOpeningQuotesMustBeOnOwnLine)
            .WithLocation(0);

        await AnalyzerVerifier<RawStringLiteralAlignmentAnalyzer>.VerifyAnalyzerAsync(source, expected, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task RegularString_NoDiagnostic()
    {
        var source =
            """""
            class C
            {
                void M()
                {
                    var s = "hello";
                }
            }
            """"";

        await AnalyzerVerifier<RawStringLiteralAlignmentAnalyzer>.VerifyAnalyzerAsync(source, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task VerbatimString_NoDiagnostic()
    {
        var source =
            """""
            class C
            {
                void M()
                {
                    var s = @"hello
                        world";
                }
            }
            """"";

        await AnalyzerVerifier<RawStringLiteralAlignmentAnalyzer>.VerifyAnalyzerAsync(source, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task InterpolatedMultiLineRawString_Dangling_Reports()
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

        var expected = AnalyzerVerifier<RawStringLiteralAlignmentAnalyzer>
            .Diagnostic(DiagnosticDescriptors.RawStringOpeningQuotesMustBeOnOwnLine)
            .WithLocation(0);

        await AnalyzerVerifier<RawStringLiteralAlignmentAnalyzer>.VerifyAnalyzerAsync(source, expected, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task InterpolatedMultiLineRawString_Aligned_NoDiagnostic()
    {
        var source =
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

        await AnalyzerVerifier<RawStringLiteralAlignmentAnalyzer>.VerifyAnalyzerAsync(source, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task InterpolatedSingleLineRawString_NoDiagnostic()
    {
        var source =
            """""
            class C
            {
                void M(string name)
                {
                    var s = $"""hello {name}""";
                }
            }
            """"";

        await AnalyzerVerifier<RawStringLiteralAlignmentAnalyzer>.VerifyAnalyzerAsync(source, TestContext.Current.CancellationToken);
    }
}
