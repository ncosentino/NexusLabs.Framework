using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;

using NexusLabs.StronglyTypedIds.Analyzers;

using Xunit;

namespace NexusLabs.Framework.Analyzers.Tests;

public sealed class UuidV7IdentifierCreationAnalyzerTests
{
    [Fact]
    public async Task NewInvocation_OnUuidV7Identifier_Reports()
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
        var expected = new DiagnosticResult("NLS0001", DiagnosticSeverity.Error)
            .WithLocation(0)
            .WithArguments("OrderId");

        await AnalyzerVerifier<UuidV7IdentifierCreationAnalyzer>
            .VerifyAnalyzerAsync(
                source,
                expected,
                TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task NewMethodGroup_OnUuidV7Identifier_Reports()
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
        var expected = new DiagnosticResult("NLS0001", DiagnosticSeverity.Error)
            .WithLocation(0)
            .WithArguments("OrderId");

        await AnalyzerVerifier<UuidV7IdentifierCreationAnalyzer>
            .VerifyAnalyzerAsync(
                source,
                expected,
                TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task UnqualifiedNewInvocation_OnUuidV7Identifier_Reports()
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
        var expected = new DiagnosticResult("NLS0001", DiagnosticSeverity.Error)
            .WithLocation(0)
            .WithArguments("OrderId");

        await AnalyzerVerifier<UuidV7IdentifierCreationAnalyzer>
            .VerifyAnalyzerAsync(
                source,
                expected,
                TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GenericNewInvocation_OnUuidV7Identifier_DoesNotReport()
    {
        var source =
            """
            namespace App
            {
                public static class C
                {
                    public static OrderId M() => OrderId.New<int>();
                }
            }
            """ + UuidV7AnalyzerTestSources.IdentifierStubs;

        await AnalyzerVerifier<UuidV7IdentifierCreationAnalyzer>
            .VerifyAnalyzerAsync(
                source,
                TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task NewInvocation_OnOtherIdentifier_DoesNotReport()
    {
        var source =
            """
            namespace App
            {
                public static class C
                {
                    public static OtherId M() => OtherId.New();
                }
            }
            """ + UuidV7AnalyzerTestSources.IdentifierStubs;

        await AnalyzerVerifier<UuidV7IdentifierCreationAnalyzer>
            .VerifyAnalyzerAsync(
                source,
                TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task NewInvocation_OnManualUuidV7Identifier_DoesNotReport()
    {
        var source =
            """
            namespace App
            {
                public static class C
                {
                    public static ManualUuidV7Id M() => ManualUuidV7Id.New();
                }
            }
            """ + UuidV7AnalyzerTestSources.IdentifierStubs;

        await AnalyzerVerifier<UuidV7IdentifierCreationAnalyzer>
            .VerifyAnalyzerAsync(
                source,
                TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task CreateInvocation_OnUuidV7Identifier_DoesNotReport()
    {
        var source =
            """
            namespace App
            {
                public static class C
                {
                    public static OrderId M() => OrderId.Create();
                }
            }
            """ + UuidV7AnalyzerTestSources.IdentifierStubs;

        await AnalyzerVerifier<UuidV7IdentifierCreationAnalyzer>
            .VerifyAnalyzerAsync(
                source,
                TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GuidNewGuidConstruction_OnUuidV7Identifier_Reports()
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
        var expected = new DiagnosticResult("NLS0002", DiagnosticSeverity.Error)
            .WithLocation(0)
            .WithArguments("OrderId");

        await AnalyzerVerifier<UuidV7IdentifierCreationAnalyzer>
            .VerifyAnalyzerAsync(
                source,
                expected,
                TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task TargetTypedGuidNewGuidConstruction_OnUuidV7Identifier_Reports()
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
        var expected = new DiagnosticResult("NLS0002", DiagnosticSeverity.Error)
            .WithLocation(0)
            .WithArguments("OrderId");

        await AnalyzerVerifier<UuidV7IdentifierCreationAnalyzer>
            .VerifyAnalyzerAsync(
                source,
                expected,
                TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ExternalGuidConstruction_OnUuidV7Identifier_DoesNotReport()
    {
        var source =
            """
            namespace App
            {
                public static class C
                {
                    public static OrderId M(System.Guid externalValue) =>
                        new OrderId(externalValue);
                }
            }
            """ + UuidV7AnalyzerTestSources.IdentifierStubs;

        await AnalyzerVerifier<UuidV7IdentifierCreationAnalyzer>
            .VerifyAnalyzerAsync(
                source,
                TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GuidNewGuidConstruction_OnOtherIdentifier_DoesNotReport()
    {
        var source =
            """
            namespace App
            {
                public static class C
                {
                    public static OtherId M() =>
                        new OtherId(System.Guid.NewGuid());
                }
            }
            """ + UuidV7AnalyzerTestSources.IdentifierStubs;

        await AnalyzerVerifier<UuidV7IdentifierCreationAnalyzer>
            .VerifyAnalyzerAsync(
                source,
                TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task NewInvocation_OnMetadataUuidV7Identifier_Reports()
    {
        var source =
            """
            namespace Consumer
            {
                public static class C
                {
                    public static App.OrderId M() => App.OrderId.{|#0:New|}();
                }
            }
            """;
        var expected = new DiagnosticResult("NLS0001", DiagnosticSeverity.Error)
            .WithLocation(0)
            .WithArguments("OrderId");

        await AnalyzerVerifier<UuidV7IdentifierCreationAnalyzer>
            .VerifyAnalyzerWithAdditionalProjectAsync(
                source,
                "Identifiers",
                [UuidV7AnalyzerTestSources.IdentifierStubs],
                expected,
                TestContext.Current.CancellationToken);
    }
}
