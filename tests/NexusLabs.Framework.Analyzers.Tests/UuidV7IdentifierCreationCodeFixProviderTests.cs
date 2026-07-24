using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

using NexusLabs.StronglyTypedIds.Analyzers;

using Xunit;

namespace NexusLabs.Framework.Analyzers.Tests;

public sealed class UuidV7IdentifierCreationCodeFixProviderTests
{
    [Fact]
    public async Task NewInvocation_ReplacesMethodName()
    {
        var source =
            """
            namespace App
            {
                public static class C
                {
                    public static OrderId M() => OrderId.{|#0:New|}();
                }
            }
            """ + UuidV7AnalyzerTestSources.IdentifierStubs;
        var fixedSource =
            """
            namespace App
            {
                public static class C
                {
                    public static OrderId M() => OrderId.Create();
                }
            }
            """ + UuidV7AnalyzerTestSources.IdentifierStubs;
        var expected = new DiagnosticResult("NLS0001", DiagnosticSeverity.Error)
            .WithLocation(0)
            .WithArguments("OrderId");

        await VerifyCodeFixAsync(
            source,
            fixedSource,
            expected,
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task NewMethodGroup_ReplacesMethodName()
    {
        var source =
            """
            namespace App
            {
                public static class C
                {
                    public static System.Func<OrderId> M() => OrderId.{|#0:New|};
                }
            }
            """ + UuidV7AnalyzerTestSources.IdentifierStubs;
        var fixedSource =
            """
            namespace App
            {
                public static class C
                {
                    public static System.Func<OrderId> M() => OrderId.Create;
                }
            }
            """ + UuidV7AnalyzerTestSources.IdentifierStubs;
        var expected = new DiagnosticResult("NLS0001", DiagnosticSeverity.Error)
            .WithLocation(0)
            .WithArguments("OrderId");

        await VerifyCodeFixAsync(
            source,
            fixedSource,
            expected,
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task UnqualifiedNewInvocation_ReplacesMethodName()
    {
        var source =
            """
            using static App.OrderId;

            namespace App
            {
                public static class C
                {
                    public static OrderId M() => {|#0:New|}();
                }
            }
            """ + UuidV7AnalyzerTestSources.IdentifierStubs;
        var fixedSource =
            """
            using static App.OrderId;

            namespace App
            {
                public static class C
                {
                    public static OrderId M() => Create();
                }
            }
            """ + UuidV7AnalyzerTestSources.IdentifierStubs;
        var expected = new DiagnosticResult("NLS0001", DiagnosticSeverity.Error)
            .WithLocation(0)
            .WithArguments("OrderId");

        await VerifyCodeFixAsync(
            source,
            fixedSource,
            expected,
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ExplicitConstruction_ReplacesWholeExpression()
    {
        var source =
            """
            namespace App
            {
                public static class C
                {
                    public static OrderId M() =>
                        {|#0:new OrderId(System.Guid.NewGuid())|};
                }
            }
            """ + UuidV7AnalyzerTestSources.IdentifierStubs;
        var fixedSource =
            """
            namespace App
            {
                public static class C
                {
                    public static OrderId M() =>
                        OrderId.Create();
                }
            }
            """ + UuidV7AnalyzerTestSources.IdentifierStubs;
        var expected = new DiagnosticResult("NLS0002", DiagnosticSeverity.Error)
            .WithLocation(0)
            .WithArguments("OrderId");

        await VerifyCodeFixAsync(
            source,
            fixedSource,
            expected,
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task TargetTypedConstruction_ReplacesWholeExpression()
    {
        var source =
            """
            namespace App
            {
                public static class C
                {
                    public static OrderId M()
                    {
                        OrderId value = {|#0:new(System.Guid.NewGuid())|};
                        return value;
                    }
                }
            }
            """ + UuidV7AnalyzerTestSources.IdentifierStubs;
        var fixedSource =
            """
            namespace App
            {
                public static class C
                {
                    public static OrderId M()
                    {
                        OrderId value = OrderId.Create();
                        return value;
                    }
                }
            }
            """ + UuidV7AnalyzerTestSources.IdentifierStubs;
        var expected = new DiagnosticResult("NLS0002", DiagnosticSeverity.Error)
            .WithLocation(0)
            .WithArguments("OrderId");

        await VerifyCodeFixAsync(
            source,
            fixedSource,
            expected,
            TestContext.Current.CancellationToken);
    }

    private static async Task VerifyCodeFixAsync(
        string source,
        string fixedSource,
        DiagnosticResult expected,
        CancellationToken cancellationToken)
    {
        var test = new CSharpCodeFixTest<
            UuidV7IdentifierCreationAnalyzer,
            UuidV7IdentifierCreationCodeFixProvider,
            DefaultVerifier>
        {
            TestCode = source,
            FixedCode = fixedSource,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
        };

        test.ExpectedDiagnostics.Add(expected);

        await test.RunAsync(cancellationToken);
    }
}
