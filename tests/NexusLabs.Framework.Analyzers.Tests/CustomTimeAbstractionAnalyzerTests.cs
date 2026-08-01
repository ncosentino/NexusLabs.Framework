using System.Threading.Tasks;

using Xunit;

namespace NexusLabs.Framework.Analyzers.Tests;

public sealed class CustomTimeAbstractionAnalyzerTests
{
    [Fact]
    public async Task ClockOnlyInterface_Reports()
    {
        var source =
            """
            using System;

            namespace App
            {
                public interface {|#0:IClock|}
                {
                    DateTimeOffset GetUtcNow();
                }
            }
            """;

        var expected = AnalyzerVerifier<CustomTimeAbstractionAnalyzer>
            .Diagnostic(DiagnosticDescriptors.DoNotDefineCustomTimeAbstraction)
            .WithLocation(0)
            .WithArguments("IClock", "1 clock member(s)");

        await AnalyzerVerifier<CustomTimeAbstractionAnalyzer>.VerifyAnalyzerAsync(
            source,
            expected,
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ClockPairedWithDelay_Reports()
    {
        var source =
            """
            using System;
            using System.Threading;
            using System.Threading.Tasks;

            namespace App
            {
                public interface {|#0:ITimeProvider|}
                {
                    DateTimeOffset GetUtcNow();

                    Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken);
                }
            }
            """;

        var expected = AnalyzerVerifier<CustomTimeAbstractionAnalyzer>
            .Diagnostic(DiagnosticDescriptors.DoNotDefineCustomTimeAbstraction)
            .WithLocation(0)
            .WithArguments("ITimeProvider", "1 clock member(s) and 1 delay member(s)");

        await AnalyzerVerifier<CustomTimeAbstractionAnalyzer>.VerifyAnalyzerAsync(
            source,
            expected,
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ClockExposedAsAProperty_Reports()
    {
        var source =
            """
            using System;

            namespace App
            {
                public interface {|#0:IClock|}
                {
                    DateTime UtcNow { get; }
                }
            }
            """;

        var expected = AnalyzerVerifier<CustomTimeAbstractionAnalyzer>
            .Diagnostic(DiagnosticDescriptors.DoNotDefineCustomTimeAbstraction)
            .WithLocation(0)
            .WithArguments("IClock", "1 clock member(s)");

        await AnalyzerVerifier<CustomTimeAbstractionAnalyzer>.VerifyAnalyzerAsync(
            source,
            expected,
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task BroaderInterfaceExposingATimestamp_ReportsNothing()
    {
        var source =
            """
            using System;
            using System.Threading.Tasks;

            namespace App
            {
                public interface IAuditSink
                {
                    DateTimeOffset GetUtcNow();

                    Task WriteAsync(string message);
                }
            }
            """;

        await AnalyzerVerifier<CustomTimeAbstractionAnalyzer>.VerifyAnalyzerAsync(
            source,
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task InterfaceWithNoClockMember_ReportsNothing()
    {
        var source =
            """
            using System;
            using System.Threading;
            using System.Threading.Tasks;

            namespace App
            {
                public interface IThrottle
                {
                    Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken);
                }
            }
            """;

        await AnalyzerVerifier<CustomTimeAbstractionAnalyzer>.VerifyAnalyzerAsync(
            source,
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task EmptyInterface_ReportsNothing()
    {
        var source =
            """
            namespace App
            {
                public interface IMarker
                {
                }
            }
            """;

        await AnalyzerVerifier<CustomTimeAbstractionAnalyzer>.VerifyAnalyzerAsync(
            source,
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ClassWithAClockMember_ReportsNothing()
    {
        var source =
            """
            using System;

            namespace App
            {
                public sealed class SystemClock
                {
                    public DateTimeOffset GetUtcNow() => DateTimeOffset.UtcNow;
                }
            }
            """;

        await AnalyzerVerifier<CustomTimeAbstractionAnalyzer>.VerifyAnalyzerAsync(
            source,
            TestContext.Current.CancellationToken);
    }
}
