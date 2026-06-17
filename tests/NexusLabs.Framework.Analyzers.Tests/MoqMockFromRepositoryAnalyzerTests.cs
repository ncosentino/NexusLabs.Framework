using System.Threading.Tasks;

using NexusLabs.Framework.Analyzers;

using Xunit;

namespace NexusLabs.Framework.Analyzers.Tests;

public sealed class MoqMockFromRepositoryAnalyzerTests
{
    [Fact]
    public async Task NewGenericMock_Reports()
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
                        var mock = {|#0:new Mock<IFoo>()|};
                    }
                }
            }
            """);

        var expected = AnalyzerVerifier<MoqMockFromRepositoryAnalyzer>
            .Diagnostic(DiagnosticDescriptors.MoqMockMustComeFromRepository)
            .WithLocation(0)
            .WithArguments("new Mock<IFoo>()");

        await AnalyzerVerifier<MoqMockFromRepositoryAnalyzer>.VerifyAnalyzerAsync(source, expected, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task NewGenericMockWithStrictBehavior_StillReports()
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
                        var mock = {|#0:new Mock<IFoo>(MockBehavior.Strict)|};
                    }
                }
            }
            """);

        var expected = AnalyzerVerifier<MoqMockFromRepositoryAnalyzer>
            .Diagnostic(DiagnosticDescriptors.MoqMockMustComeFromRepository)
            .WithLocation(0)
            .WithArguments("new Mock<IFoo>()");

        await AnalyzerVerifier<MoqMockFromRepositoryAnalyzer>.VerifyAnalyzerAsync(source, expected, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task TargetTypedNewMock_Reports()
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
                        Mock<IFoo> mock = {|#0:new()|};
                    }
                }
            }
            """);

        var expected = AnalyzerVerifier<MoqMockFromRepositoryAnalyzer>
            .Diagnostic(DiagnosticDescriptors.MoqMockMustComeFromRepository)
            .WithLocation(0)
            .WithArguments("new Mock<IFoo>()");

        await AnalyzerVerifier<MoqMockFromRepositoryAnalyzer>.VerifyAnalyzerAsync(source, expected, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task MockOf_Reports()
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
                        var foo = {|#0:Mock.Of<IFoo>()|};
                    }
                }
            }
            """);

        var expected = AnalyzerVerifier<MoqMockFromRepositoryAnalyzer>
            .Diagnostic(DiagnosticDescriptors.MoqMockMustComeFromRepository)
            .WithLocation(0)
            .WithArguments("Mock.Of<IFoo>()");

        await AnalyzerVerifier<MoqMockFromRepositoryAnalyzer>.VerifyAnalyzerAsync(source, expected, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task CreateFromRepository_NoDiagnostic()
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

        await AnalyzerVerifier<MoqMockFromRepositoryAnalyzer>.VerifyAnalyzerAsync(source, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task UserTypeNamedMockInOtherNamespace_NoDiagnostic()
    {
        var source =
            """
            namespace NotMoq
            {
                public sealed class Mock<T> { }
            }

            namespace App
            {
                using NotMoq;

                public sealed class C
                {
                    public void M()
                    {
                        var mock = new Mock<string>();
                    }
                }
            }
            """;

        await AnalyzerVerifier<MoqMockFromRepositoryAnalyzer>.VerifyAnalyzerAsync(source, TestContext.Current.CancellationToken);
    }
}
