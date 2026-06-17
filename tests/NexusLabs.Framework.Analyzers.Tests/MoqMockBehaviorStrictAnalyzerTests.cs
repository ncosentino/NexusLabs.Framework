using System.Threading.Tasks;

using NexusLabs.Framework.Analyzers;

using Xunit;

namespace NexusLabs.Framework.Analyzers.Tests;

public sealed class MoqMockBehaviorStrictAnalyzerTests
{
    [Fact]
    public async Task RepositoryLoose_Reports()
    {
        var source = MoqTestSource.Wrap(
            """
            using Moq;

            namespace App
            {
                public sealed class C
                {
                    private readonly MockRepository _mocks = new({|#0:MockBehavior.Loose|});
                }
            }
            """);

        var expected = AnalyzerVerifier<MoqMockBehaviorStrictAnalyzer>
            .Diagnostic(DiagnosticDescriptors.MoqMockBehaviorMustBeStrict)
            .WithLocation(0)
            .WithArguments("MockBehavior.Loose", "Loose");

        await AnalyzerVerifier<MoqMockBehaviorStrictAnalyzer>.VerifyAnalyzerAsync(source, expected, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task RepositoryDefault_Reports()
    {
        var source = MoqTestSource.Wrap(
            """
            using Moq;

            namespace App
            {
                public sealed class C
                {
                    private readonly MockRepository _mocks = new MockRepository({|#0:MockBehavior.Default|});
                }
            }
            """);

        var expected = AnalyzerVerifier<MoqMockBehaviorStrictAnalyzer>
            .Diagnostic(DiagnosticDescriptors.MoqMockBehaviorMustBeStrict)
            .WithLocation(0)
            .WithArguments("MockBehavior.Default", "Default");

        await AnalyzerVerifier<MoqMockBehaviorStrictAnalyzer>.VerifyAnalyzerAsync(source, expected, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task RepositoryStrict_NoDiagnostic()
    {
        var source = MoqTestSource.Wrap(
            """
            using Moq;

            namespace App
            {
                public sealed class C
                {
                    private readonly MockRepository _mocks = new(MockBehavior.Strict);
                }
            }
            """);

        await AnalyzerVerifier<MoqMockBehaviorStrictAnalyzer>.VerifyAnalyzerAsync(source, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task CreateOverrideLoose_Reports()
    {
        var source = MoqTestSource.Wrap(
            """
            using Moq;

            namespace App
            {
                public interface IFoo { }

                public sealed class C
                {
                    private readonly MockRepository _mocks = new(MockBehavior.Strict);

                    public void M()
                    {
                        var mock = _mocks.Create<IFoo>({|#0:MockBehavior.Loose|});
                    }
                }
            }
            """);

        var expected = AnalyzerVerifier<MoqMockBehaviorStrictAnalyzer>
            .Diagnostic(DiagnosticDescriptors.MoqMockBehaviorMustBeStrict)
            .WithLocation(0)
            .WithArguments("MockBehavior.Loose", "Loose");

        await AnalyzerVerifier<MoqMockBehaviorStrictAnalyzer>.VerifyAnalyzerAsync(source, expected, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task CreateNoBehaviorOverride_NoDiagnostic()
    {
        var source = MoqTestSource.Wrap(
            """
            using Moq;

            namespace App
            {
                public interface IFoo { }

                public sealed class C
                {
                    private readonly MockRepository _mocks = new(MockBehavior.Strict);

                    public void M()
                    {
                        var mock = _mocks.Create<IFoo>();
                    }
                }
            }
            """);

        await AnalyzerVerifier<MoqMockBehaviorStrictAnalyzer>.VerifyAnalyzerAsync(source, TestContext.Current.CancellationToken);
    }
}
