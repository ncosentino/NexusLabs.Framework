using System.Threading.Tasks;

using NexusLabs.Framework.Analyzers;

using Xunit;

namespace NexusLabs.Framework.Analyzers.Tests;

public sealed class MoqItIsAnyValueTypeAnalyzerTests
{
    [Fact]
    public async Task IsAnyInt_Reports()
    {
        var source = MoqTestSource.Wrap(
            """
            using Moq;

            namespace App
            {
                public sealed class C
                {
                    public void M()
                    {
                        var x = {|#0:It.IsAny<int>()|};
                    }
                }
            }
            """);

        var expected = AnalyzerVerifier<MoqItIsAnyValueTypeAnalyzer>
            .Diagnostic(DiagnosticDescriptors.MoqItIsAnyOnValueTypeOrRecord)
            .WithLocation(0)
            .WithArguments("int", "value type");

        await AnalyzerVerifier<MoqItIsAnyValueTypeAnalyzer>.VerifyAnalyzerAsync(source, expected, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task IsAnyEnum_Reports()
    {
        var source = MoqTestSource.Wrap(
            """
            using Moq;

            namespace App
            {
                public enum Color { Red, Green }

                public sealed class C
                {
                    public void M()
                    {
                        var x = {|#0:It.IsAny<Color>()|};
                    }
                }
            }
            """);

        var expected = AnalyzerVerifier<MoqItIsAnyValueTypeAnalyzer>
            .Diagnostic(DiagnosticDescriptors.MoqItIsAnyOnValueTypeOrRecord)
            .WithLocation(0)
            .WithArguments("Color", "value type");

        await AnalyzerVerifier<MoqItIsAnyValueTypeAnalyzer>.VerifyAnalyzerAsync(source, expected, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task IsAnyStruct_Reports()
    {
        var source = MoqTestSource.Wrap(
            """
            using System;
            using Moq;

            namespace App
            {
                public sealed class C
                {
                    public void M()
                    {
                        var x = {|#0:It.IsAny<Guid>()|};
                    }
                }
            }
            """);

        var expected = AnalyzerVerifier<MoqItIsAnyValueTypeAnalyzer>
            .Diagnostic(DiagnosticDescriptors.MoqItIsAnyOnValueTypeOrRecord)
            .WithLocation(0)
            .WithArguments("Guid", "value type");

        await AnalyzerVerifier<MoqItIsAnyValueTypeAnalyzer>.VerifyAnalyzerAsync(source, expected, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task IsAnyRecord_Reports()
    {
        var source = MoqTestSource.Wrap(
            """
            using Moq;

            namespace App
            {
                public record Point(int X, int Y);

                public sealed class C
                {
                    public void M()
                    {
                        var x = {|#0:It.IsAny<Point>()|};
                    }
                }
            }
            """);

        var expected = AnalyzerVerifier<MoqItIsAnyValueTypeAnalyzer>
            .Diagnostic(DiagnosticDescriptors.MoqItIsAnyOnValueTypeOrRecord)
            .WithLocation(0)
            .WithArguments("Point", "record");

        await AnalyzerVerifier<MoqItIsAnyValueTypeAnalyzer>.VerifyAnalyzerAsync(source, expected, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task IsAnyRecordStruct_Reports()
    {
        var source = MoqTestSource.Wrap(
            """
            using Moq;

            namespace App
            {
                public record struct Vec(int X);

                public sealed class C
                {
                    public void M()
                    {
                        var x = {|#0:It.IsAny<Vec>()|};
                    }
                }
            }
            """);

        var expected = AnalyzerVerifier<MoqItIsAnyValueTypeAnalyzer>
            .Diagnostic(DiagnosticDescriptors.MoqItIsAnyOnValueTypeOrRecord)
            .WithLocation(0)
            .WithArguments("Vec", "record");

        await AnalyzerVerifier<MoqItIsAnyValueTypeAnalyzer>.VerifyAnalyzerAsync(source, expected, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task IsAnyCancellationToken_NoDiagnostic()
    {
        var source = MoqTestSource.Wrap(
            """
            using System.Threading;
            using Moq;

            namespace App
            {
                public sealed class C
                {
                    public void M()
                    {
                        var x = It.IsAny<CancellationToken>();
                    }
                }
            }
            """);

        await AnalyzerVerifier<MoqItIsAnyValueTypeAnalyzer>.VerifyAnalyzerAsync(source, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task IsAnyReferenceTypes_NoDiagnostic()
    {
        var source = MoqTestSource.Wrap(
            """
            using Moq;

            namespace App
            {
                public interface IFoo { }

                public sealed class C
                {
                    public void M()
                    {
                        var s = It.IsAny<string>();
                        var f = It.IsAny<IFoo>();
                    }
                }
            }
            """);

        await AnalyzerVerifier<MoqItIsAnyValueTypeAnalyzer>.VerifyAnalyzerAsync(source, TestContext.Current.CancellationToken);
    }
}
