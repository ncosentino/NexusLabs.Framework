using System.Threading.Tasks;

using Microsoft.CodeAnalysis.Testing;

using Xunit;

namespace NexusLabs.Framework.Analyzers.Tests;

public sealed class FakeClockTimerOverrideAnalyzerTests
{
    private static readonly PackageIdentity[] _timeProviderTesting =
    [
        new("Microsoft.Extensions.TimeProvider.Testing", "10.8.0"),
    ];

    [Fact]
    public async Task OverridesCreateTimer_Reports()
    {
        var source =
            """
            using System;
            using System.Threading;
            using System.Threading.Tasks;
            using Microsoft.Extensions.Time.Testing;

            namespace App
            {
                public sealed class ImmediateFakeClock : FakeTimeProvider
                {
                    public override ITimer {|#0:CreateTimer|}(
                        TimerCallback callback,
                        object? state,
                        TimeSpan dueTime,
                        TimeSpan period) => new Immediate(callback, state);

                    private sealed class Immediate : ITimer
                    {
                        public Immediate(TimerCallback callback, object? state) => callback(state);

                        public bool Change(TimeSpan dueTime, TimeSpan period) => true;

                        public void Dispose()
                        {
                        }

                        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
                    }
                }
            }
            """;

        var expected = AnalyzerVerifier<FakeClockTimerOverrideAnalyzer>
            .Diagnostic(DiagnosticDescriptors.DoNotOverrideFakeClockCreateTimer)
            .WithLocation(0)
            .WithArguments("ImmediateFakeClock");

        await AnalyzerVerifier<FakeClockTimerOverrideAnalyzer>.VerifyAnalyzerWithPackagesAsync(
            source,
            _timeProviderTesting,
            [expected],
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task IndirectSubclassReplacingTheTimer_Reports()
    {
        var source =
            """
            using System;
            using System.Threading;
            using System.Threading.Tasks;
            using Microsoft.Extensions.Time.Testing;

            namespace App
            {
                public class LabelledFakeClock : FakeTimeProvider
                {
                    public string Label { get; init; } = "test";
                }

                public sealed class DerivedFakeClock : LabelledFakeClock
                {
                    public override ITimer {|#0:CreateTimer|}(
                        TimerCallback callback,
                        object? state,
                        TimeSpan dueTime,
                        TimeSpan period) => new Immediate(callback, state);

                    private sealed class Immediate : ITimer
                    {
                        public Immediate(TimerCallback callback, object? state) => callback(state);

                        public bool Change(TimeSpan dueTime, TimeSpan period) => true;

                        public void Dispose()
                        {
                        }

                        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
                    }
                }
            }
            """;

        var expected = AnalyzerVerifier<FakeClockTimerOverrideAnalyzer>
            .Diagnostic(DiagnosticDescriptors.DoNotOverrideFakeClockCreateTimer)
            .WithLocation(0)
            .WithArguments("DerivedFakeClock");

        await AnalyzerVerifier<FakeClockTimerOverrideAnalyzer>.VerifyAnalyzerWithPackagesAsync(
            source,
            _timeProviderTesting,
            [expected],
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task OverrideThatDelegatesToBase_ReportsNothing()
    {
        var source =
            """
            using System;
            using System.Threading;
            using System.Threading.Tasks;
            using Microsoft.Extensions.Time.Testing;

            namespace App
            {
                public sealed class ObservingFakeClock : FakeTimeProvider
                {
                    public int CreateTimerCalls { get; private set; }

                    public override ITimer CreateTimer(
                        TimerCallback callback,
                        object? state,
                        TimeSpan dueTime,
                        TimeSpan period)
                    {
                        var inner = base.CreateTimer(callback, state, dueTime, period);
                        CreateTimerCalls++;
                        return inner;
                    }
                }
            }
            """;

        await AnalyzerVerifier<FakeClockTimerOverrideAnalyzer>.VerifyAnalyzerWithPackagesAsync(
            source,
            _timeProviderTesting,
            [],
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ExpressionBodiedOverrideThatDelegatesToBase_ReportsNothing()
    {
        var source =
            """
            using System;
            using System.Threading;
            using Microsoft.Extensions.Time.Testing;

            namespace App
            {
                public sealed class PassThroughFakeClock : FakeTimeProvider
                {
                    public override ITimer CreateTimer(
                        TimerCallback callback,
                        object? state,
                        TimeSpan dueTime,
                        TimeSpan period) => base.CreateTimer(callback, state, dueTime, period);
                }
            }
            """;

        await AnalyzerVerifier<FakeClockTimerOverrideAnalyzer>.VerifyAnalyzerWithPackagesAsync(
            source,
            _timeProviderTesting,
            [],
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task SubclassThatDoesNotTouchScheduling_ReportsNothing()
    {
        var source =
            """
            using Microsoft.Extensions.Time.Testing;

            namespace App
            {
                public sealed class LabelledFakeClock : FakeTimeProvider
                {
                    public string Label { get; init; } = "test";
                }
            }
            """;

        await AnalyzerVerifier<FakeClockTimerOverrideAnalyzer>.VerifyAnalyzerWithPackagesAsync(
            source,
            _timeProviderTesting,
            [],
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task UnrelatedTimeProviderOverridingCreateTimer_ReportsNothing()
    {
        var source =
            """
            using System;
            using System.Threading;

            namespace App
            {
                public sealed class CustomProvider : TimeProvider
                {
                    public override ITimer CreateTimer(
                        TimerCallback callback,
                        object? state,
                        TimeSpan dueTime,
                        TimeSpan period) => base.CreateTimer(callback, state, dueTime, period);
                }
            }
            """;

        await AnalyzerVerifier<FakeClockTimerOverrideAnalyzer>.VerifyAnalyzerWithPackagesAsync(
            source,
            _timeProviderTesting,
            [],
            TestContext.Current.CancellationToken);
    }
}
