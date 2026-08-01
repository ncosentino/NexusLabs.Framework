using System.Threading.Tasks;

using Microsoft.CodeAnalysis.Testing;

using Xunit;

namespace NexusLabs.Framework.Analyzers.Tests;

public sealed class TimeProviderFlowAnalyzerTests
{
    [Fact]
    public async Task TaskDelayWithClockInScope_ReportsForwardRule()
    {
        var source =
            """
            using System;
            using System.Threading;
            using System.Threading.Tasks;

            namespace App
            {
                public sealed class C
                {
                    private readonly TimeProvider _timeProvider;

                    public C(TimeProvider timeProvider) => _timeProvider = timeProvider;

                    public Task DoAsync(CancellationToken cancellationToken)
                        => {|#0:Task.Delay(TimeSpan.FromMinutes(1), cancellationToken)|};
                }
            }
            """;

        var expected = AnalyzerVerifier<TimeProviderFlowAnalyzer>
            .Diagnostic(DiagnosticDescriptors.ForwardAvailableTimeProvider)
            .WithLocation(0)
            .WithArguments("Task.Delay", "_timeProvider, timeProvider");

        await AnalyzerVerifier<TimeProviderFlowAnalyzer>.VerifyAnalyzerAsync(
            source,
            expected,
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task CancellationTokenSourceWithClockInScope_ReportsForwardRule()
    {
        var source =
            """
            using System;
            using System.Threading;

            namespace App
            {
                public sealed class C
                {
                    private readonly TimeProvider _timeProvider;

                    public C(TimeProvider timeProvider) => _timeProvider = timeProvider;

                    public CancellationTokenSource Create()
                        => {|#0:new CancellationTokenSource(TimeSpan.FromSeconds(30))|};
                }
            }
            """;

        var expected = AnalyzerVerifier<TimeProviderFlowAnalyzer>
            .Diagnostic(DiagnosticDescriptors.ForwardAvailableTimeProvider)
            .WithLocation(0)
            .WithArguments("CancellationTokenSource", "_timeProvider, timeProvider");

        await AnalyzerVerifier<TimeProviderFlowAnalyzer>.VerifyAnalyzerAsync(
            source,
            expected,
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task PeriodicTimerWithPrimaryConstructorClock_ReportsForwardRule()
    {
        var source =
            """
            using System;
            using System.Threading;

            namespace App
            {
                public sealed class C(TimeProvider timeProvider)
                {
                    public PeriodicTimer Create()
                        => {|#0:new PeriodicTimer(TimeSpan.FromSeconds(5))|};
                }
            }
            """;

        var expected = AnalyzerVerifier<TimeProviderFlowAnalyzer>
            .Diagnostic(DiagnosticDescriptors.ForwardAvailableTimeProvider)
            .WithLocation(0)
            .WithArguments("PeriodicTimer", "timeProvider");

        await AnalyzerVerifier<TimeProviderFlowAnalyzer>.VerifyAnalyzerAsync(
            source,
            expected,
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task MethodParameterClock_ReportsForwardRule()
    {
        var source =
            """
            using System;
            using System.Threading;
            using System.Threading.Tasks;

            namespace App
            {
                public static class C
                {
                    public static Task DoAsync(TimeProvider clock, CancellationToken cancellationToken)
                        => {|#0:Task.Delay(TimeSpan.FromMinutes(1), cancellationToken)|};
                }
            }
            """;

        var expected = AnalyzerVerifier<TimeProviderFlowAnalyzer>
            .Diagnostic(DiagnosticDescriptors.ForwardAvailableTimeProvider)
            .WithLocation(0)
            .WithArguments("Task.Delay", "clock");

        await AnalyzerVerifier<TimeProviderFlowAnalyzer>.VerifyAnalyzerAsync(
            source,
            expected,
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task NoClockInScope_ReportsTheWeakerRuleInstead()
    {
        var source =
            """
            using System;
            using System.Threading.Tasks;

            namespace App
            {
                public static class C
                {
                    public static Task PauseAsync()
                        => {|#0:Task.Delay(TimeSpan.FromMinutes(1))|};
                }
            }
            """;

        var expected = AnalyzerVerifier<TimeProviderFlowAnalyzer>
            .Diagnostic(DiagnosticDescriptors.TimeProviderOverloadAvailable)
            .WithLocation(0)
            .WithArguments("Task.Delay");

        await AnalyzerVerifier<TimeProviderFlowAnalyzer>.VerifyAnalyzerAsync(
            source,
            expected,
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ClockForwarded_ReportsNothing()
    {
        var source =
            """
            using System;
            using System.Threading;
            using System.Threading.Tasks;

            namespace App
            {
                public sealed class C
                {
                    private readonly TimeProvider _timeProvider;

                    public C(TimeProvider timeProvider) => _timeProvider = timeProvider;

                    public async Task DoAsync(CancellationToken cancellationToken)
                    {
                        await Task.Delay(TimeSpan.FromMinutes(1), _timeProvider, cancellationToken);
                        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30), _timeProvider);
                        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5), _timeProvider);
                    }
                }
            }
            """;

        await AnalyzerVerifier<TimeProviderFlowAnalyzer>.VerifyAnalyzerAsync(
            source,
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task StaticClockOnABaseType_IsNotTreatedAsAnAvailableClock()
    {
        // Anything deriving from TimeProvider inherits the static TimeProvider.System. Offering
        // that as "a clock is already available" would be advice to bind to the machine clock.
        var source =
            """
            using System;
            using System.Threading;
            using System.Threading.Tasks;

            namespace App
            {
                public sealed class C : TimeProvider
                {
                    public Task WaitAsync(Task work, CancellationToken cancellationToken)
                        => {|#0:work.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken)|};
                }
            }
            """;

        var expected = AnalyzerVerifier<TimeProviderFlowAnalyzer>
            .Diagnostic(DiagnosticDescriptors.TimeProviderOverloadAvailable)
            .WithLocation(0)
            .WithArguments("Task.WaitAsync");

        await AnalyzerVerifier<TimeProviderFlowAnalyzer>.VerifyAnalyzerAsync(
            source,
            expected,
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task CallWithoutATimeProviderOverload_ReportsNothing()
    {
        var source =
            """
            using System;
            using System.Threading.Tasks;

            namespace App
            {
                public sealed class C
                {
                    private readonly TimeProvider _timeProvider;

                    public C(TimeProvider timeProvider) => _timeProvider = timeProvider;

                    public string Describe() => _timeProvider.GetUtcNow().ToString("O");
                }
            }
            """;

        await AnalyzerVerifier<TimeProviderFlowAnalyzer>.VerifyAnalyzerAsync(
            source,
            TestContext.Current.CancellationToken);
    }
}
